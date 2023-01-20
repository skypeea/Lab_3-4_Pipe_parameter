using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab_3_4_Pipe_parameter
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            var categorySet = new CategorySet();
            categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeCurves));
            using (Transaction ts = new Transaction(doc, "Add parameter"))
            {
                ts.Start();
                CreateSharedParameter(uiapp.Application, doc, "Наименование", categorySet, BuiltInParameterGroup.PG_TEXT, true);
                ts.Commit();
            }

            var pipeList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WhereElementIsNotElementType()
                .Cast<Pipe>()
                .ToList();

            using (Transaction ts = new Transaction(doc, "Set parameters"))
            {
                ts.Start();

                foreach (var selectedElement in pipeList)
                {
                    
                    if (selectedElement is Pipe)
                    {
                        
                        Parameter outerDiameterParameter = selectedElement.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER);
                        Parameter innerDiameterParameter = selectedElement.get_Parameter(BuiltInParameter.RBS_PIPE_INNER_DIAM_PARAM);
                        Parameter name = selectedElement.LookupParameter("Наименование");
                        double d1 = UnitUtils.ConvertFromInternalUnits(outerDiameterParameter.AsDouble(), UnitTypeId.Millimeters);
                        double d2 = UnitUtils.ConvertFromInternalUnits(innerDiameterParameter.AsDouble(), UnitTypeId.Millimeters);

                        name.Set($"Труба {d1.ToString()} / {d2.ToString()}");
                    }
                }

                ts.Commit();
            }

            return Result.Succeeded;
        }
        private void CreateSharedParameter(Autodesk.Revit.ApplicationServices.Application application,
           Document doc, string parameterName, CategorySet categorySet,
           BuiltInParameterGroup builtInParameterGroup, bool isInstance)
        {
            DefinitionFile definitionFile = application.OpenSharedParameterFile();
            if (definitionFile == null)
            {
                TaskDialog.Show("Ошибка", "Не найден файл общих параметров");
                return;
            }

            Definition definition = definitionFile.Groups
                .SelectMany(group => group.Definitions)
                .FirstOrDefault(def => def.Name.Equals(parameterName));

            if (definition == null)
            {
                TaskDialog.Show("Ошибка", "Не найден указанный параметр");
                return;
            }
            Binding binding = application.Create.NewTypeBinding(categorySet);
            if (isInstance)
                binding = application.Create.NewInstanceBinding(categorySet);
            BindingMap map = doc.ParameterBindings;
            map.Insert(definition, binding, builtInParameterGroup);
        }
    }
}