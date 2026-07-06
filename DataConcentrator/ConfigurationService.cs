using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using DataConcentrator.Model;

namespace DataConcentrator
{
    // ImportResult se vraca nakon uvoza konfiguracije kako bi pozivalac znao:
    // - koliko je tagova uspjesno uvezeno
    // - koji su tagovi preskoceni i zbog cega (duplikat, nevalidni podaci, itd.)
    // - koji su ulazni tagovi (AI/DI) novododati, da ih pozivalac moze odmah pokrenuti
    public class ImportResult
    {
        public int Imported { get; set; }
        public List<string> Skipped { get; } = new List<string>();
        public List<Tag> NewlyAddedInputs { get; } = new List<Tag>();
    }

    public static class ConfigurationService
    {
        // regex za validaciju imena taga - mora pocinjati slovom, zatim slova/brojevi/donja crta
        // isto pravilo kao za rucni unos u AddWindow
        private static readonly Regex NameRegex = new Regex(@"^[A-Za-z][A-Za-z0-9_]*$");

        // izvozi sve tagove iz baze u JSON fajl na zadatoj putanji
        // redosled: AI -> AO -> DI -> DO
        public static void ExportToJson(string filePath)
        {
            var context = ContextClass.Instance;

            // lista u koju se pakuju svi tagovi bez obzira na tip
            var tags = new List<TagExportModel>();

            // prolazi kroz svaki tip taga i pretvara ga u TagExportModel
            // TagExportModel je "ravna" klasa koja moze da predstavi bilo koji tip taga
            foreach (var ai in context.Tags.OfType<AnalogInput>().ToList())
                tags.Add(new TagExportModel
                {
                    Type = "AI",
                    Name = ai.Name,
                    Description = ai.Description,
                    IOAddress = ai.IOAddress,
                    ScanTime = ai.ScanTime,
                    IsScanning = ai.IsScanning,
                    LowLimit = ai.LowLimit,
                    HighLimit = ai.HighLimit,
                    Units = ai.Units,
                    Deadband = ai.Deadband,
                    Hysteresis = ai.Hysteresis
                });

            foreach (var ao in context.Tags.OfType<AnalogOutput>().ToList())
                tags.Add(new TagExportModel
                {
                    Type = "AO",
                    Name = ao.Name,
                    Description = ao.Description,
                    IOAddress = ao.IOAddress,
                    InitialValue = ao.InitialValue,
                    LowLimit = ao.LowLimit,
                    HighLimit = ao.HighLimit,
                    Units = ao.Units
                });

            foreach (var di in context.Tags.OfType<DigitalInput>().ToList())
                tags.Add(new TagExportModel
                {
                    Type = "DI",
                    Name = di.Name,
                    Description = di.Description,
                    IOAddress = di.IOAddress,
                    ScanTime = di.ScanTime,
                    IsScanning = di.IsScanning
                });

            foreach (var dout in context.Tags.OfType<DigitalOutput>().ToList())
                tags.Add(new TagExportModel
                {
                    Type = "DO",
                    Name = dout.Name,
                    Description = dout.Description,
                    IOAddress = dout.IOAddress,
                    InitialValue = dout.InitialValue
                });

            // pakuje listu tagova u ConfigurationExport koji je koren JSON strukture
            var export = new ConfigurationExport { Tags = tags };
            var serializer = new DataContractJsonSerializer(typeof(ConfigurationExport));

            // using automatski zatvara i oslobada fajl stream na kraju bloka
            // FileMode.Create pravi novi fajl ili prepisuje postojeci
            using (var stream = File.Open(filePath, FileMode.Create))
                serializer.WriteObject(stream, export);

            // belezi akciju u system.log ako je ImportExport bit postavljen u TraceWordu
            Logger.Log(Logger.LogType.ImportExport, $"Configuration exported to {filePath} ({tags.Count} tags)");
        }

        // uvozi tagove iz JSON fajla u bazu
        // vraca ImportResult sa statistikom uvoza
        public static ImportResult ImportFromJson(string filePath)
        {
            var result = new ImportResult();

            var serializer = new DataContractJsonSerializer(typeof(ConfigurationExport));
            ConfigurationExport export;

            // TrimStart uklanja BOM (byte order mark) koji neki editori dodaju na pocetak UTF-8 fajla
            // bez ovoga deserijalizacija bi pukla jer JSON ne ocekuje BOM na pocetku
            string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8).TrimStart('﻿');
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
                export = (ConfigurationExport)serializer.ReadObject(stream);

            // ako je fajl prazan ili nema tagova, vrati prazan rezultat
            if (export?.Tags == null)
                return result;

            var context = ContextClass.Instance;

            // ime taga je [Key] na baznoj Tag klasi (deljeno izmedju svih tipova), pa se
            // provera duplikata mora raditi globalno, ne samo unutar istog podtipa.
            // Isto vazi za adresu - jedna adresa moze biti dodeljena samo jednom tagu.
            // HashSet omogucava O(1) proveru da li element vec postoji.
            // Skupovi se dopunjuju u hodu kako bi se uhvatili i duplikati unutar samog JSON fajla.
            var usedNames = new HashSet<string>(context.Tags.Select(t => t.Name));
            var usedAddresses = new HashSet<string>(context.Tags.Select(t => t.IOAddress));

            foreach (var dto in export.Tags)
            {
                // proveri da li je tag validan pre dodavanja u bazu
                // Validate vraca null ako je dto ispravan, inace poruku greske
                string error = Validate(dto, usedNames, usedAddresses);
                if (error != null)
                {
                    // zabeleži preskoceni tag sa razlogom i nastavi sa sledecim
                    result.Skipped.Add($"{dto.Name} ({dto.Type}): {error}");
                    continue;
                }

                // na osnovu Type polja kreira odgovarajuci objekat
                Tag tag;
                switch (dto.Type)
                {
                    case "AI":
                        tag = new AnalogInput
                        {
                            Name = dto.Name,
                            Description = dto.Description,
                            IOAddress = dto.IOAddress,
                            ScanTime = dto.ScanTime,
                            IsScanning = dto.IsScanning,
                            LowLimit = dto.LowLimit,
                            HighLimit = dto.HighLimit,
                            Units = dto.Units,
                            Deadband = dto.Deadband,
                            Hysteresis = dto.Hysteresis
                        };
                        // AI je ulazni tag - dodaje se u NewlyAddedInputs da pozivalac moze pokrenuti skeniranje
                        result.NewlyAddedInputs.Add(tag);
                        break;

                    case "AO":
                        tag = new AnalogOutput
                        {
                            Name = dto.Name,
                            Description = dto.Description,
                            IOAddress = dto.IOAddress,
                            InitialValue = dto.InitialValue,
                            LowLimit = dto.LowLimit,
                            HighLimit = dto.HighLimit,
                            Units = dto.Units
                        };
                        // AO je izlazni tag - nema skeniranja, ne dodaje se u NewlyAddedInputs
                        break;

                    case "DI":
                        tag = new DigitalInput
                        {
                            Name = dto.Name,
                            Description = dto.Description,
                            IOAddress = dto.IOAddress,
                            ScanTime = dto.ScanTime,
                            IsScanning = dto.IsScanning
                        };
                        // DI je ulazni tag - dodaje se u NewlyAddedInputs
                        result.NewlyAddedInputs.Add(tag);
                        break;

                    case "DO":
                        tag = new DigitalOutput { Name = dto.Name, Description = dto.Description, IOAddress = dto.IOAddress, InitialValue = dto.InitialValue };
                        // DO je izlazni tag - nema skeniranja
                        break;

                    default:
                        // nepoznat tip - preskoci i zabeleži
                        result.Skipped.Add($"{dto.Name}: nepoznat tip taga '{dto.Type}'");
                        continue;
                }

                context.Tags.Add(tag);

                // ažuriraj skupove da sledeci tag u JSON fajlu ne dobije isti naziv/adresu
                usedNames.Add(dto.Name);
                usedAddresses.Add(dto.IOAddress);
                result.Imported++;
            }

            // jedan SaveChanges na kraju je efikasniji od pozivanja nakon svakog taga
            context.SaveChanges();
            Logger.Log(Logger.LogType.ImportExport,
                $"Configuration imported from {filePath} ({result.Imported} tags, {result.Skipped.Count} skipped)");
            return result;
        }

        // validacija jednog taga iz JSON fajla
        // ista pravila kao u AddWindow.BtnAdd_Click, primenjena i na uvezene tagove
        // vraca null ako je dto ispravan, inace string sa opisom greske
        private static string Validate(TagExportModel dto, HashSet<string> usedNames, HashSet<string> usedAddresses)
        {
            // provera imena - ne sme biti prazno, ne duze od 50 znakova, mora odgovarati regex-u
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 50 || !NameRegex.IsMatch(dto.Name))
                return "neispravno ime taga";

            // provera duplikata - ime mora biti jedinstveno u celoj bazi
            if (usedNames.Contains(dto.Name))
                return "ime već postoji";

            // provera adrese - mora biti zadata
            if (string.IsNullOrEmpty(dto.IOAddress))
                return "nedostaje I/O adresa";

            // provera duplikata adrese - jedna adresa moze biti dodeljena samo jednom tagu
            if (usedAddresses.Contains(dto.IOAddress))
                return "I/O adresa je već zauzeta";

            // provera specificnih polja zavisno od tipa taga
            switch (dto.Type)
            {
                case "AI":
                    if (dto.ScanTime <= 0) return "scan time mora biti veći od 0";
                    if (dto.HighLimit <= dto.LowLimit) return "high limit mora biti veći od low limit";
                    if (dto.Deadband < 0) return "deadband ne može biti negativan";
                    if (dto.Hysteresis < 0) return "hysteresis ne može biti negativan";
                    return null;    // sve proslo - tag je validan

                case "AO":
                    if (dto.HighLimit <= dto.LowLimit) return "high limit mora biti veći od low limit";
                    // initial value mora biti unutar zadatog opsega
                    if (dto.InitialValue < dto.LowLimit || dto.InitialValue > dto.HighLimit) return "initial value van opsega low/high limit";
                    return null;

                case "DI":
                    if (dto.ScanTime <= 0) return "scan time mora biti veći od 0";
                    return null;

                case "DO":
                    // digitalni izlaz moze biti samo 0 (iskljuceno) ili 1 (ukljuceno)
                    if (dto.InitialValue != 0 && dto.InitialValue != 1) return "digitalni izlaz može biti samo 0 ili 1";
                    return null;

                default:
                    return "nepoznat tip taga";
            }
        }
    }
}