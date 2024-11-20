using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using ClosedXML.Excel;

class Program
{
    public class FileLocation
    {
        public string Name { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public Location StartLoc { get; set; }
        public Location EndLoc { get; set; }
    }

    public class Location
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Position { get; set; }
    }

    public class Duplicate
    {
        public string Format { get; set; }
        public int Lines { get; set; }
        public string Fragment { get; set; }
        public int Tokens { get; set; }
        public FileLocation FirstFile { get; set; }
        public FileLocation SecondFile { get; set; }
    }

    public class JsonData
    {
        public List<Duplicate> Duplicates { get; set; }
    }

    static void Main(string[] args)
    {
        string excelFilePath = "Results.xlsx"; // Specify your Excel file path here
        string jsonFilePath = "path_to_your_json_file.json";  // Specify your JSON file path here

        // Load and process the JSON file
        var jsonData = ReadJsonFile(jsonFilePath);
        Console.WriteLine("Processing duplicates...");

        // Process duplicates
        var (reusableComponents, falsePositives) = ProcessDuplicates(jsonData.Duplicates);

        // Write the results to Excel
        WriteToExcel(excelFilePath, reusableComponents, falsePositives);

        // Inform the user that the output has been written to the Excel file
        Console.WriteLine($"Results written to {excelFilePath}");
    }

    static JsonData ReadJsonFile(string filePath)
    {
        string jsonContent = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<JsonData>(jsonContent);
    }

    static (List<Duplicate>, List<Duplicate>) ProcessDuplicates(List<Duplicate> duplicates)
    {
        var reusableComponents = new List<Duplicate>();
        var falsePositives = new List<Duplicate>();

        foreach (var duplication in duplicates)
        {
            if (duplication.FirstFile?.Name != duplication.SecondFile?.Name)
            {
                reusableComponents.Add(duplication);
            }

            if (IsFalsePositive(duplication.Fragment))
            {
                falsePositives.Add(duplication);
            }
        }

        return (reusableComponents, falsePositives);
    }

    static bool IsFalsePositive(string fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment))
            return false;

        string[] falsePositivePatterns = new[]
        {
            "global using", "//", "#include", "using", "Mock<", "TestClass", "UnitTest", 
            "Assert.", "namespace ", "public class", "private", "protected", "Generated by"
        };

        string[] testFileIndicators = new[]
        {
            "UnitTest", "Test", "HandlerTest", "ControllerTest", "ServiceTest"
        };

        if (falsePositivePatterns.Any(pattern => fragment.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (testFileIndicators.Any(indicator => fragment.Contains(indicator, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    static void WriteToExcel(string filePath, List<Duplicate> reusableComponents, List<Duplicate> falsePositives)
    {
        using (var workbook = new XLWorkbook())
        {
            // Add sheets
            var reusableSheet = workbook.Worksheets.Add("Reusable Components");
            var falsePositivesSheet = workbook.Worksheets.Add("False Positives");

            // Write Reusable Components
            WriteDuplicatesToSheet(reusableSheet, reusableComponents, "Reusable Component");

            // Write False Positives
            WriteDuplicatesToSheet(falsePositivesSheet, falsePositives, "False Positive");

            // Save workbook
            workbook.SaveAs(filePath);
        }
    }

    static void WriteDuplicatesToSheet(IXLWorksheet sheet, List<Duplicate> duplicates, string title)
    {
        // Header
        sheet.Cell(1, 1).Value = "ID";
        sheet.Cell(1, 2).Value = "Fragment";
        sheet.Cell(1, 3).Value = "First File Name";
        sheet.Cell(1, 4).Value = "Second File Name";

        // Write data
        int row = 2;
        foreach (var item in duplicates)
        {
            sheet.Cell(row, 1).Value = row - 1;
            sheet.Cell(row, 2).Value = item.Fragment;
            sheet.Cell(row, 3).Value = item.FirstFile?.Name;
            sheet.Cell(row, 4).Value = item.SecondFile?.Name;
            row++;
        }

        // Adjust column widths
        sheet.Columns().AdjustToContents();
    }
}
