using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using System.Collections.Generic;
using System.Linq;

namespace PhaseToParam
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class PhaseParam : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                Transaction t = new Transaction(doc, "Update Elements Parameters");

                // get all categories in the document
                Categories categories = doc.Settings.Categories;

                // get all elements from model categories, availabe in the project
                FilteredElementCollector elemCollector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
                List<Element> modelElements = new List<Element>();

                foreach (Element element in elemCollector)
                    if (element != null && element.Category != null)
                        if (element.Category.CategoryType == CategoryType.Model)
                            modelElements.Add(element);
                
                // check if parameter is loaded for all model elements
                string output = "Parameter PhaseData is either not loaded or not assigned to all model categories, available in the current document. Please assign it to the following categories:\n";
                bool isNotLoaded = false;
                List<string> catNames = new List<string>();

                foreach (Element element in modelElements)
                {                    
                    Parameter phaseDataCheck = element.LookupParameter("PhaseData");
                    if (phaseDataCheck == null)
                    {
                        if (!catNames.Contains(element.Category.Name))
                        {
                            catNames.Add(element.Category.Name);
                            output += $"\n{element.Category.Name}";
                            isNotLoaded = true;
                        }
                    }
                }
                if (isNotLoaded)
                {
                    TaskDialog.Show("Issues Found", output);
                    return Result.Succeeded;
                }
                else
                {
                    output = "Phase Data was updated for all model elements";
                    t.Start();
                    foreach (var elem in modelElements)
                    {
                        string phaseCreated = elem.get_Parameter(BuiltInParameter.PHASE_CREATED).AsValueString();
                        string phaseDemolished = elem.get_Parameter(BuiltInParameter.PHASE_DEMOLISHED).AsValueString();

                        if (phaseCreated != null)
                        {
                            if (phaseCreated == "Existing")
                            {
                                if (phaseDemolished == "None")
                                    elem.LookupParameter("PhaseData").Set("Existing");
                                else
                                    elem.LookupParameter("PhaseData").Set("Demolished");
                            }
                            else if (phaseCreated.Contains("Phase"))
                            {
                                if (phaseDemolished == "None")
                                    elem.LookupParameter("PhaseData").Set($"Created {phaseCreated}");
                                else
                                    elem.LookupParameter("PhaseData").Set("Demolished");
                            }
                        }
                        else
                        {
                            output += $"\nException: {elem.Id}";
                            elem.LookupParameter("PhaseData").Set("Phase created is null");
                        }
                    }
                    t.Commit();

                    TaskDialog.Show("Task Completed", output);
                    return Result.Succeeded;
                }                
            }
            catch (Exception e)
            {
                TaskDialog.Show("Exception", e.ToString());
                return Result.Failed;
            }
        }
    }
}
