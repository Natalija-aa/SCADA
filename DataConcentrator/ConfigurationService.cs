using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using DataConcentrator.Model;

namespace DataConcentrator
{
    public static class ConfigurationService
    {
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

            using (var stream = File.Open(filePath, FileMode.Create))
                serializer.WriteObject(stream, export);

            Logger.Log(Logger.LogType.ImportExport, $"Configuration exported to {filePath} ({tags.Count} tags)");
        }

        public static void ImportFromJson(string filePath)
        {
            var serializer = new DataContractJsonSerializer(typeof(ConfigurationExport));
            ConfigurationExport export;

            string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8).TrimStart('﻿');
            using (var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
                export = (ConfigurationExport)serializer.ReadObject(stream);

            if (export?.Tags == null)
                return;

            var context = ContextClass.Instance;

            foreach (var dto in export.Tags)
            {
                switch (dto.Type)
                {
                    case "AI":
                        if (context.Tags.OfType<AnalogInput>().FirstOrDefault(t => t.Name == dto.Name) == null)
                            context.Tags.Add(new AnalogInput
                            {
                                Name = dto.Name, Description = dto.Description,
                                IOAddress = dto.IOAddress, ScanTime = dto.ScanTime,
                                IsScanning = dto.IsScanning, LowLimit = dto.LowLimit,
                                HighLimit = dto.HighLimit, Units = dto.Units,
                                Deadband = dto.Deadband, Hysteresis = dto.Hysteresis
                            });
                        break;

                    case "AO":
                        if (context.Tags.OfType<AnalogOutput>().FirstOrDefault(t => t.Name == dto.Name) == null)
                            context.Tags.Add(new AnalogOutput
                            {
                                Name = dto.Name, Description = dto.Description,
                                IOAddress = dto.IOAddress, InitialValue = dto.InitialValue,
                                LowLimit = dto.LowLimit, HighLimit = dto.HighLimit, Units = dto.Units
                            });
                        break;

                    case "DI":
                        if (context.Tags.OfType<DigitalInput>().FirstOrDefault(t => t.Name == dto.Name) == null)
                            context.Tags.Add(new DigitalInput
                            {
                                Name = dto.Name, Description = dto.Description,
                                IOAddress = dto.IOAddress, ScanTime = dto.ScanTime,
                                IsScanning = dto.IsScanning
                            });
                        break;

                    case "DO":
                        if (context.Tags.OfType<DigitalOutput>().FirstOrDefault(t => t.Name == dto.Name) == null)
                            context.Tags.Add(new DigitalOutput
                            {
                                Name = dto.Name, Description = dto.Description,
                                IOAddress = dto.IOAddress, InitialValue = dto.InitialValue
                            });
                        break;
                }
            }

            context.SaveChanges();
            Logger.Log(Logger.LogType.ImportExport, $"Configuration imported from {filePath} ({export.Tags.Count} tags)");
        }
    }
}
