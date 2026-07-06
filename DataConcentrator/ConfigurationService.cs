using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using DataConcentrator.Model;

namespace DataConcentrator
{
    // rezultat uvoza konfiguracije - koliko je tagova uvezeno, koji su preskoceni i zasto,
    // i koji su ulazni tagovi novododati (da pozivalac moze da pokrene njihovo skeniranje)
    public class ImportResult
    {
        public int Imported { get; set; }
        public List<string> Skipped { get; } = new List<string>();
        public List<Tag> NewlyAddedInputs { get; } = new List<Tag>();
    }

    public static class ConfigurationService
    {
        // isto pravilo kao za rucni unos u AddWindow
        private static readonly Regex NameRegex = new Regex(@"^[A-Za-z][A-Za-z0-9_]*$");

        // izvozi sve tagve iz baze u JSON fajl
        public static void ExportToJson(string filePath)
        {
            var context = ContextClass.Instance;
            var tags = new List<TagExportModel>();

            foreach (var ai in context.Tags.OfType<AnalogInput>().ToList())
                tags.Add(new TagExportModel
                {
                    Type = "AI", Name = ai.Name, Description = ai.Description,
                    IOAddress = ai.IOAddress, ScanTime = ai.ScanTime,
                    IsScanning = ai.IsScanning, LowLimit = ai.LowLimit,
                    HighLimit = ai.HighLimit, Units = ai.Units,
                    Deadband = ai.Deadband, Hysteresis = ai.Hysteresis
                });

            foreach (var ao in context.Tags.OfType<AnalogOutput>().ToList())
                tags.Add(new TagExportModel
                {
                    Type = "AO", Name = ao.Name, Description = ao.Description,
                    IOAddress = ao.IOAddress, InitialValue = ao.InitialValue,
                    LowLimit = ao.LowLimit, HighLimit = ao.HighLimit, Units = ao.Units
                });

            foreach (var di in context.Tags.OfType<DigitalInput>().ToList())
                tags.Add(new TagExportModel
                {
                    Type = "DI", Name = di.Name, Description = di.Description,
                    IOAddress = di.IOAddress, ScanTime = di.ScanTime,
                    IsScanning = di.IsScanning
                });

            foreach (var dout in context.Tags.OfType<DigitalOutput>().ToList())
                tags.Add(new TagExportModel
                {
                    Type = "DO", Name = dout.Name, Description = dout.Description,
                    IOAddress = dout.IOAddress, InitialValue = dout.InitialValue
                });

            var export = new ConfigurationExport { Tags = tags };
            var serializer = new DataContractJsonSerializer(typeof(ConfigurationExport));

            // using - automatsji zatvara fajl stream na kraju bloka
            using (var stream = File.Open(filePath, FileMode.Create))
                serializer.WriteObject(stream, export);

            Logger.Log(Logger.LogType.ImportExport, $"Configuration exported to {filePath} ({tags.Count} tags)");
        }

        public static ImportResult ImportFromJson(string filePath)
        {
            var result = new ImportResult();

            var serializer = new DataContractJsonSerializer(typeof(ConfigurationExport));
            ConfigurationExport export;

            string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8).TrimStart('﻿');
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
                export = (ConfigurationExport)serializer.ReadObject(stream);

            if (export?.Tags == null)
                return result;

            var context = ContextClass.Instance;

            // ime taga je [Key] na baznoj Tag klasi (deljeno izmedju svih tipova), pa se
            // i provera duplikata mora raditi globalno, ne samo unutar istog podtipa;
            // isto vazi za adresu - ne sme se poklopiti sa vec postojecim tagom.
            // Skupovi se dopunjuju u hodu da bi se uhvatili i duplikati unutar samog JSON fajla.
            var usedNames = new HashSet<string>(context.Tags.Select(t => t.Name));
            var usedAddresses = new HashSet<string>(context.Tags.Select(t => t.IOAddress));

            foreach (var dto in export.Tags)
            {
                string error = Validate(dto, usedNames, usedAddresses);
                if (error != null)
                {
                    result.Skipped.Add($"{dto.Name} ({dto.Type}): {error}");
                    continue;
                }

                Tag tag;
                switch (dto.Type)
                {
                    case "AI":
                        tag = new AnalogInput
                        {
                            Name = dto.Name, Description = dto.Description,
                            IOAddress = dto.IOAddress, ScanTime = dto.ScanTime,
                            IsScanning = dto.IsScanning, LowLimit = dto.LowLimit,
                            HighLimit = dto.HighLimit, Units = dto.Units,
                            Deadband = dto.Deadband, Hysteresis = dto.Hysteresis
                        };
                        result.NewlyAddedInputs.Add(tag);
                        break;

                    case "AO":
                        tag = new AnalogOutput
                        {
                            Name = dto.Name, Description = dto.Description,
                            IOAddress = dto.IOAddress, InitialValue = dto.InitialValue,
                            LowLimit = dto.LowLimit, HighLimit = dto.HighLimit, Units = dto.Units
                        };
                        break;

                    case "DI":
                        tag = new DigitalInput
                        {
                            Name = dto.Name, Description = dto.Description,
                            IOAddress = dto.IOAddress, ScanTime = dto.ScanTime,
                            IsScanning = dto.IsScanning
                        };
                        result.NewlyAddedInputs.Add(tag);
                        break;

                    case "DO":
                        tag = new DigitalOutput { Name = dto.Name, Description = dto.Description, IOAddress = dto.IOAddress, InitialValue = dto.InitialValue };
                        break;

                    default:
                        result.Skipped.Add($"{dto.Name}: nepoznat tip taga '{dto.Type}'");
                        continue;
                }

                context.Tags.Add(tag);
                usedNames.Add(dto.Name);
                usedAddresses.Add(dto.IOAddress);
                result.Imported++;
            }

            context.SaveChanges();
            Logger.Log(Logger.LogType.ImportExport,
                $"Configuration imported from {filePath} ({result.Imported} tags, {result.Skipped.Count} skipped)");
            return result;
        }

        // ista pravila kao u AddWindow.BtnAdd_Click, primenjena i na uvezene tagove
        // vraca null ako je dto ispravan, inace poruku razloga za preskakanje
        private static string Validate(TagExportModel dto, HashSet<string> usedNames, HashSet<string> usedAddresses)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 50 || !NameRegex.IsMatch(dto.Name))
                return "neispravno ime taga";
            if (usedNames.Contains(dto.Name))
                return "ime već postoji";
            if (string.IsNullOrEmpty(dto.IOAddress))
                return "nedostaje I/O adresa";
            if (usedAddresses.Contains(dto.IOAddress))
                return "I/O adresa je već zauzeta";

            switch (dto.Type)
            {
                case "AI":
                    if (dto.ScanTime <= 0) return "scan time mora biti veći od 0";
                    if (dto.HighLimit <= dto.LowLimit) return "high limit mora biti veći od low limit";
                    if (dto.Deadband < 0) return "deadband ne može biti negativan";
                    if (dto.Hysteresis < 0) return "hysteresis ne može biti negativan";
                    return null;

                case "AO":
                    if (dto.HighLimit <= dto.LowLimit) return "high limit mora biti veći od low limit";
                    if (dto.InitialValue < dto.LowLimit || dto.InitialValue > dto.HighLimit) return "initial value van opsega low/high limit";
                    return null;

                case "DI":
                    if (dto.ScanTime <= 0) return "scan time mora biti veći od 0";
                    return null;

                case "DO":
                    if (dto.InitialValue != 0 && dto.InitialValue != 1) return "digitalni izlaz može biti samo 0 ili 1";
                    return null;

                default:
                    return "nepoznat tip taga";
            }
        }
    }
}
