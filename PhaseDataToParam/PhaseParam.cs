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

                List<BuiltInCategory> categoriesCollection = new List<BuiltInCategory>();
                categoriesCollection.Add(BuiltInCategory.OST_Walls);
                categoriesCollection.Add(BuiltInCategory.OST_Doors);
                categoriesCollection.Add(BuiltInCategory.OST_Windows);

                ElementMulticategoryFilter multicategoryFilter = new ElementMulticategoryFilter(categoriesCollection);

                FilteredElementCollector elemCollector = new FilteredElementCollector(doc).WherePasses(multicategoryFilter).WhereElementIsNotElementType();
                
                string output = "PhaseData was updated for all Walls, Doors and Windows in the project";

                t.Start();
                foreach (var elem in elemCollector)
                {
                    string phaseCreated = elem.get_Parameter(BuiltInParameter.PHASE_CREATED).AsValueString();
                    string phaseDemolished = elem.get_Parameter(BuiltInParameter.PHASE_DEMOLISHED).AsValueString();

                    Parameter phaseDataCheck = elem.LookupParameter("PhaseData");
                    if (phaseDataCheck == null)
                    {
                        output = "Parameter PhaseData is either not loaded or not assigned to all of the following categories: Walls, Doors, Windows";
                        break;
                    }

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
                        elem.LookupParameter("Comments").Set("This was null");
                }
                t.Commit();

                TaskDialog.Show("Report", output);
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
