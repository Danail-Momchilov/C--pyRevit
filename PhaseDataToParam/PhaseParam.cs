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

                FilteredElementCollector wallsCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsNotElementType();
                
                string phases = "";

                t.Start();
                foreach (Wall wall in wallsCollector)
                {
                    string phaseCreated = wall.get_Parameter(BuiltInParameter.PHASE_CREATED).AsValueString();
                    phases = phases + phaseCreated + "\n";
                    string phaseDemolished = wall.get_Parameter(BuiltInParameter.PHASE_DEMOLISHED).AsValueString();
                    phases = phases + phaseDemolished + "\n";

                    if (phaseCreated != null)
                    {
                        if (phaseCreated == "Existing")
                        {
                            switch (phaseDemolished)
                            {
                                case "Phase 1":
                                    wall.LookupParameter("Comments").Set("Demo P1");
                                    break;
                                case "Phase 2":
                                    wall.LookupParameter("Comments").Set("Demo P2");
                                    break;
                                case "Phase 3":
                                    wall.LookupParameter("Comments").Set("Demo P3");
                                    break;
                                case "None":
                                    wall.LookupParameter("Comments").Set("Existing");
                                    break;
                                default:
                                    break;
                            }
                        }
                        else if (phaseCreated.Contains("Phase"))
                        {
                            if (phaseDemolished.Contains("Phase"))
                                wall.LookupParameter("Comments").Set("Temp");
                            else if (phaseDemolished == "None")
                                wall.LookupParameter("Comments").Set("New P" + wall.get_Parameter(BuiltInParameter.PHASE_CREATED).AsValueString().Last());
                        }
                        else if (phaseCreated == "New Construction")
                            wall.LookupParameter("Comments").Set("New");
                    }
                    else
                        wall.LookupParameter("Comments").Set("This was null");
                }
                t.Commit();

                TaskDialog.Show("Title1", phases);
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