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

                // list with 'allowed' categories, that will be skipped by the program
                List<string> allowedCats = new List<string>() {
                    "Materials", "Primary Contours", "Project Information", "Internal Origin", "HVAC Zones", "Survey Point", "Project Base Point", "Sun Path", "Material Assest", "Sheets", "Cameras",
                    "RVT Links", "Curtain Wall Grids","<Sketch>","Rectangular Straight Wall Opening","Railing Rail Path Extension Lines","Roof opening cut","Curtain Roof Grids","Curtain System Grids",
                    "<Room Separation>","Rooms","Legend Components","Material Assets","Pipe Segments","Lines","Runs","Landings","Top Rails","Balusters","Fascias","Curtain Systems","Duct Systems",
                    "Center Line","Conduit Runs","Raster Images","Piping Systems","Center line"
                };

                // get all elements from model categories, availabe in the project
                FilteredElementCollector elemCollector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
                List<Element> modelElements = new List<Element>();

                foreach (Element element in elemCollector)
                    if (element != null && element.Category != null)
                        if (element.Category.CategoryType == CategoryType.Model && !allowedCats.Contains(element.Category.Name) && !element.Category.Name.Contains(".dwg"))
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
                                
                int elemCounter = 0;
                
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
                        Parameter phaseCreated = elem.get_Parameter(BuiltInParameter.PHASE_CREATED);
                        Parameter phaseDemolished = elem.get_Parameter(BuiltInParameter.PHASE_DEMOLISHED);

                        if (phaseCreated != null && phaseDemolished != null)
                        {
                            string phaseCreatedTxt = phaseCreated.AsValueString();
                            string phaseDemolishedTxt = phaseDemolished.AsValueString();

                            if (phaseCreatedTxt == "Existing")
                            {
                                if (phaseDemolishedTxt == "None")
                                    elem.LookupParameter("PhaseData").Set("Existing");
                                else
                                    elem.LookupParameter("PhaseData").Set($"Demolished {phaseDemolishedTxt}");
                            }
                            else
                            {
                                if (phaseDemolishedTxt == "None")
                                    elem.LookupParameter("PhaseData").Set($"Created {phaseCreatedTxt}");
                                else
                                    elem.LookupParameter("PhaseData").Set($"Demolished {phaseDemolishedTxt}");
                            }
                            elemCounter++;
                        }
                        else
                        {
                            if (!output.Contains(elem.Category.Name))
                                output += $"\nException: {elem.Id} / {elem.Category.Name}";
                        }
                    }
                }
                t.Commit();

                output += $"\nA total number of {elemCounter} elements' parameters were updated";                    

                TaskDialog.Show("Task Completed", output);
                return Result.Succeeded;          
            }
            catch (Exception e)
            {
                TaskDialog.Show("Exception", e.ToString());
                return Result.Failed;
            }
        }
    }
}
