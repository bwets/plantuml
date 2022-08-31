using bwets.PlantUML.Structurizr.Definitions;
using bwets.PlantUML.Structurizr.ModelExtensions;
using Structurizr;

namespace bwets.PlantUML.Structurizr
{
    public class C4PlantUmlWriter : PlantUMLWriterBase
    {
        public string CustomC4BaseUrl { get; set; } = string.Empty;

        public C4PlantUmlWriter(Workspace workspace) : base(workspace)
        {
        }

        protected override void Write(SystemLandscapeView view, string path)
        {
            using var writer = GetWriter(view, ViewTypes.SystemLandscape, path);
            var showBoundary = view.EnterpriseBoundaryVisible ?? true;
            
            WriteProlog(view, writer);

            view.Elements
                .Select(ev => ev.Element)
                .Where(e => e is Person { Location: Location.External })
                .OrderBy(e => e.Name).ToList()
                .ForEach(e => Write(e, writer));

            view.Elements
                .Select(ev => ev.Element)
                .Where(e => e is SoftwareSystem { Location: Location.External })
                .OrderBy(e => e.Name).ToList()
                .ForEach(e => Write(e, writer));

            if (showBoundary)
            {
                var enterpriseName = view.Model.Enterprise.Name;
                writer.WriteLine($"Enterprise_Boundary({TokenizeName(enterpriseName)}, \"{enterpriseName}\") {{");
            }

            view.Elements
                .Select(ev => ev.Element)
                .Where(e => e is Person { Location: Location.Internal })
                .OrderBy(e => e.Name).ToList()
                .ForEach(e => Write(e, writer, showBoundary ? 1 : 0));

            view.Elements
                .Select(ev => ev.Element)
                .Where(e => e is SoftwareSystem { Location: Location.Internal })
                .OrderBy(e => e.Name).ToList()
                .ForEach(e => Write(e, writer, showBoundary ? 1 : 0));

            if (showBoundary)
                writer.WriteLine("}");

            Write(view.Relationships, writer);


            WriteEpilog(view, writer);
        }

        protected override void Write(SystemContextView view, string path)
        {
            using var writer = GetWriter(view, ViewTypes.SystemContext, path);
            var showBoundary = view.EnterpriseBoundaryVisible ?? true;
            
            WriteProlog(view,  writer);

            if (showBoundary)
            {
                var enterpriseName = view.Model.Enterprise.Name;
                writer.WriteLine($"Enterprise_Boundary({TokenizeName(enterpriseName)}, \"{enterpriseName}\") {{");
            }

            view.Elements
                .Select(ev => ev.Element)
                .OrderBy(e => e.Name).ToList()
                .ForEach(e => Write(e, writer, showBoundary ? 1 : 0));
            Write(view.Relationships, writer);

            if (showBoundary)
                writer.WriteLine("}");

            WriteEpilog(view, writer);
        }

        protected override void Write(ContainerView view, string path)
        {
            using var writer = GetWriter(view, ViewTypes.Container, path);
            var externals = view.Elements
                .Select(ev => ev.Element)
                .Where(e => e is not Container).ToArray();
            var showBoundary = externals.Any();

            WriteProlog(view, writer);

            externals
                .OrderBy(e => e.Name).ToList()
                .ForEach(e => Write(e, writer));

            if (showBoundary)
                Write(view.SoftwareSystem, writer, 0, true);

            view.Elements
                .Select(ev => ev.Element)
                .Where(e => e is Container)
                .OrderBy(e => e.Name).ToList()
                .ForEach(e => Write(e, writer, showBoundary ? 1 : 0));

            if (showBoundary)
                writer.WriteLine("}");

            Write(view.Relationships, writer);

            WriteEpilog(view, writer);
        }

        protected override void Write(ComponentView view, string path)
        {
            using var writer = GetWriter(view, ViewTypes.Component, path);

            var nonComponents = view.Elements
                .Select(ev => ev.Element)
                .Where(e => e is not Component)
                .ToArray();
            var nonContainedComponents = view.Elements
                .Select(ev => new { ev, e = ev.Element })
                .Where(@t => @t.e is Component && @t.e.Parent?.Id != view.Container.Id)
                .GroupBy(@t => @t.e.Parent, @t => @t.e)
                .ToArray();
            var showBoundary = nonComponents.Any() || nonContainedComponents.Any();

            WriteProlog(view, writer);

            nonComponents
                .OrderBy(e => e.Name).ToList()
                .ForEach(e => Write(e, writer));

            if (showBoundary)
                Write(view.Container, writer, 0, true);

            view.Elements
                .Select(ev => ev.Element)
                .Where(e => e is Component && e.Parent?.Id == view.Container.Id)
                .OrderBy(e => e.Name).ToList()
                .ForEach(e => Write(e, writer, showBoundary ? 1 : 0));

            if (showBoundary)
                writer.WriteLine("}");

            foreach (var container in nonContainedComponents)
            {
                Write(container.Key, writer, 0, true);

                container
                    .OrderBy(e => e.Name).ToList()
                    .ForEach(e => Write(e, writer, 1));

                writer.WriteLine("}");
            }

            Write(view.Relationships, writer);

            WriteEpilog(view, writer);
        }

        protected override void Write(DynamicView view, string path)
        {
            using var writer = GetWriter(view, ViewTypes.Dynamic, path);

            WriteProlog(view, writer);

            IList<Element> innerElements = new List<Element>();
            IList<Element> outerElements = new List<Element>();

            if (view.Element != null)
            {
                // boundary check via parent
                var parentId = view.Element.Id;
                view.Elements
                    .Select(ev => ev.Element)
                    .OrderBy(e => e.Name).ToList()
                    .ForEach(e =>
                    {
                        if (e?.Parent?.Id == parentId || e?.Parent?.Parent?.Id == parentId ||
                            e?.Parent?.Parent?.Parent?.Id == parentId)
                            innerElements.Add(e);
                        else
                            outerElements.Add(e);
                    });
            }
            else
            {
                // TODO: model based boundary checks are missing
                view.Elements
                    .Select(ev => ev.Element)
                    .OrderBy(e => e.Name).ToList()
                    .ForEach(e => innerElements.Add(e));
            }

            var showBoundary = outerElements.Count > 0;

            outerElements
                .OrderBy(e => e.Name).ToList()
                .ForEach(e => Write(e, writer, 0));

            if (showBoundary)
                Write(view.Element, writer, 0, true);

            innerElements
                .OrderBy(e => e.Name).ToList()
                .ForEach(e => Write(e, writer, showBoundary ? 1 : 0));

            if (showBoundary)
                writer.WriteLine("}");

            WriteDynamicInteractions(view.Relationships, writer);

            WriteEpilog(view, writer);
        }

        protected override void Write(DeploymentView view, string path)
        {
            using var writer = GetWriter(view, ViewTypes.Deployment, path);
            WriteProlog(view, writer);

            view.Elements
                .Where(ev => ev.Element is DeploymentNode && ev.Element.Parent == null)
                .Select(ev => ev.Element as DeploymentNode)
                .OrderBy(e => e.Name).ToList()
                .ForEach(e => Write(e, writer, 0));

            Write(view.Relationships, writer);

            WriteEpilog(view, writer);
        }

        private void Write(DeploymentNode deploymentNode, TextWriter writer, int indentLevel)
        {
            var indent = indentLevel == 0 ? string.Empty : new string(' ', indentLevel * 2);

            Write(deploymentNode, writer, indentLevel, true);

            foreach (DeploymentNode child in deploymentNode.Children)
            {
                Write(child, writer, indentLevel + 1);
            }

            foreach (ContainerInstance containerInstance in deploymentNode.ContainerInstances)
            {
                Write(containerInstance, writer, indentLevel + 1);
            }

            writer.WriteLine($"{indent}}}");
        }

        
        private string TypeOf(Element e)
        {
            return e switch
            {
                Person => "Person",
                SoftwareSystem => "Software System",
                Container => "Container",
                Component component => ValueOrDefault(component.Technology,"Component"),
                DeploymentNode node => ValueOrDefault(node.Technology,"Deployment Node"),
                ContainerInstance => "Container",
                _ => string.Empty
            };
        }

        private string ValueOrDefault(string s, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(s) ? defaultValue : s;
        }

        protected override void WriteProlog(View view, TextWriter writer)
        {
            writer.WriteLine("@startuml");

            switch (view)
            {
                case SystemLandscapeView _:
                case SystemContextView _:
                    writer.WriteLine(!string.IsNullOrWhiteSpace(CustomC4BaseUrl)
                        ? $"!includeurl {CustomC4BaseUrl}C4_Context.puml"
                        : $"!include <C4/C4_Context>");
                    break;

                case ComponentView _:
                    writer.WriteLine(!string.IsNullOrWhiteSpace(CustomC4BaseUrl)
                        ? $"!includeurl {CustomC4BaseUrl}C4_Component.puml"
                        : $"!include <C4/C4_Component>");
                    break;

                case DynamicView _:
                    if (!string.IsNullOrWhiteSpace(CustomC4BaseUrl))
                    {
                        writer.WriteLine($"!includeurl {CustomC4BaseUrl}C4_Dynamic.puml");
                    }
                    else
                    {
                        writer.WriteLine(@"!include <C4/C4_Component>");
                        // Add missing deployment nodes (until they are part of the plantuml macros)
                        writer.WriteLine(@"' C4_Dynamic.puml is missing, simulate it with following definitions");

                        writer.WriteLine(@"' Scope: Interactions in an enterprise, software system or container.");
                        writer.WriteLine(@"' Primary and supporting elements: Depends on the diagram scope - ");
                        writer.WriteLine(@"'     enterprise - people and software systems related to the enterprise in scope ");
                        writer.WriteLine(@"'     software system - see system context or container diagrams, ");
                        writer.WriteLine(@"'     container - see component diagram.");
                        writer.WriteLine(@"' Intended audience: Technical and non-technical people, inside and outside of the software development team.");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"' Dynamic diagram introduces (automatically) numbered interactions: ");
                        writer.WriteLine(@"'     Interact(): used automatic calculated index, ");
                        writer.WriteLine(@"'     Interact2(): index can be explicit defined,");
                        writer.WriteLine(@"'     SetIndex(): set the next index, ");
                        writer.WriteLine(@"'     GetIndex(): get the index and automatically increase index");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"' Index");
                        writer.WriteLine(@"' ##################################");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!function $inc_($value, $step=1)");
                        writer.WriteLine(@"  !return $value + $step");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!$index=1");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!function SetIndex($new_index)");
                        writer.WriteLine(@"  !$index=$new_index");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!function GetIndex($auto_increase=1)");
                        writer.WriteLine(@"  !$old = $index");
                        writer.WriteLine(@"  !$index=$inc_($index, $auto_increase)");
                        writer.WriteLine(@"  !return $old");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"' Interact");
                        writer.WriteLine(@"' ##################################");
                        writer.WriteLine(@"!define Interact2(e_index, e_from, e_to, e_label) Rel(e_from, e_to, ""e_index: e_label"")");
                        writer.WriteLine(@"!define Interact2(e_index, e_from, e_to, e_label, e_techn) Rel(e_from, e_to, ""e_index: e_label"", e_techn)");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!define Interact2_Back(e_index, e_from, e_to, e_label) Rel_Back(e_from, e_to, ""e_index: e_label"")");
                        writer.WriteLine(@"!define Interact2_Back(e_index, e_from, e_to, e_label, e_techn) Rel_Back(e_from, e_to, ""e_index: e_label"", e_techn)");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!define Interact2_Neighbor(e_index, e_from, e_to, e_label) Rel_Neighbor(e_from, e_to, ""e_index: e_label"")");
                        writer.WriteLine(@"!define Interact2_Neighbor(e_index, e_from, e_to, e_label, e_techn) Rel_Neighbor(e_from, e_to, ""e_index: e_label"", e_techn)");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!define Interact2_Back_Neighbor(e_index, e_from, e_to, e_label) Rel_Back_Neighbor(e_from, e_to, ""e_index: e_label"")");
                        writer.WriteLine(@"!define Interact2_Back_Neighbor(e_index, e_from, e_to, e_label, e_techn) Rel_Back_Neighbor(e_from, e_to, ""e_index: e_label"", e_techn)");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!define Interact2_D(e_index, e_from, e_to, e_label) Rel_D(e_from, e_to, ""e_index: e_label"")");
                        writer.WriteLine(@"!define Interact2_D(e_index, e_from, e_to, e_label, e_techn) Rel_D(e_from, e_to, ""e_index: e_label"", e_techn)");
                        writer.WriteLine(@"!define Interact2_Down(e_index, e_from, e_to, e_label) Rel_Down(e_from, e_to, ""e_index: e_label"")");
                        writer.WriteLine(@"!define Interact2_Down(e_index, e_from, e_to, e_label, e_techn) Rel_Down(e_from, e_to, ""e_index: e_label"", e_techn)");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!define Interact2_U(e_index, e_from, e_to, e_label) Rel_U(e_from, e_to, ""e_index: e_label"")");
                        writer.WriteLine(@"!define Interact2_U(e_index, e_from, e_to, e_label, e_techn) Rel_U(e_from, e_to, ""e_index: e_label"", e_techn)");
                        writer.WriteLine(@"!define Interact2_Up(e_index, e_from, e_to, e_label) Rel_Up(e_from, e_to, ""e_index: e_label"")");
                        writer.WriteLine(@"!define Interact2_Up(e_index, e_from, e_to, e_label, e_techn) Rel_Up(e_from, e_to, ""e_index: e_label"", e_techn)");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!define Interact2_L(e_index, e_from, e_to, e_label) Rel_L(e_from, e_to, ""e_index: e_label"")");
                        writer.WriteLine(@"!define Interact2_L(e_index, e_from, e_to, e_label, e_techn) Rel_L(e_from, e_to, ""e_index: e_label"", e_techn)");
                        writer.WriteLine(@"!define Interact2_Left(e_index, e_from, e_to, e_label) Rel_Left(e_from, e_to, ""e_index: e_label"")");
                        writer.WriteLine(@"!define Interact2_Left(e_index, e_from, e_to, e_label, e_techn) Rel_Left(e_from, e_to, ""e_index: e_label"", e_techn)");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!define Interact2_R(e_index, e_from, e_to, e_label) Rel_R(e_from, e_to, ""e_index: e_label"")");
                        writer.WriteLine(@"!define Interact2_R(e_index, e_from, e_to, e_label, e_techn) Rel_R(e_from, e_to, ""e_index: e_label"", e_techn)");
                        writer.WriteLine(@"!define Interact2_Right(e_index, e_from, e_to, e_label) Rel_Right(e_from, e_to, ""e_index: e_label"")");
                        writer.WriteLine(@"!define Interact2_Right(e_index, e_from, e_to, e_label, e_techn) Rel_Right(e_from, e_to, ""e_index: e_label"", e_techn)");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!unquoted function Interact($e_from, $e_to, $e_label) ");
                        writer.WriteLine(@"  Interact2($index, ""$e_from"", ""$e_to"", ""$e_label"")");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact($e_from, $e_to, $e_label, $e_techn) ");
                        writer.WriteLine(@"  Interact2($index, ""$e_from"", ""$e_to"", ""$e_label"", $e_techn)");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!unquoted function Interact_Back($e_from, $e_to, $e_label) ");
                        writer.WriteLine(@"  Interact2_Back($index, ""$e_from"", ""$e_to"", ""$e_label"")");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_Back($e_from, $e_to, $e_label, $e_techn) ");
                        writer.WriteLine(@"  Interact2_Back($index, ""$e_from"", ""$e_to"", ""$e_label"", $e_techn)");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!unquoted function Interact_Neighbor($e_from, $e_to, $e_label) ");
                        writer.WriteLine(@"  Interact2_Neighbor($index, ""$e_from"", ""$e_to"", ""$e_label"")");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_Neighbor($e_from, $e_to, $e_label, $e_techn) ");
                        writer.WriteLine(@"  Interact2_Neighbor($index, ""$e_from"", ""$e_to"", ""$e_label"", $e_techn)");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!unquoted function Interact_Back_Neighbor($e_from, $e_to, $e_label) ");
                        writer.WriteLine(@"  Interact2_Back_Neighbor($index, ""$e_from"", ""$e_to"", ""$e_label"")");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_Back_Neighbor($e_from, $e_to, $e_label, $e_techn) ");
                        writer.WriteLine(@"  Interact2_Back_Neighbor($index, ""$e_from"", ""$e_to"", ""$e_label"", $e_techn)");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!unquoted function Interact_D($e_from, $e_to, $e_label) ");
                        writer.WriteLine(@"  Interact2_D($index, ""$e_from"", ""$e_to"", ""$e_label"")");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_D($e_from, $e_to, $e_label, $e_techn) ");
                        writer.WriteLine(@"  Interact2_D($index, ""$e_from"", ""$e_to"", ""$e_label"", $e_techn)");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_Down($e_from, $e_to, $e_label) ");
                        writer.WriteLine(@"  Interact2_Down($index, ""$e_from"", ""$e_to"", ""$e_label"")");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_Down($e_from, $e_to, $e_label, $e_techn) ");
                        writer.WriteLine(@"  Interact2_Down($index, ""$e_from"", ""$e_to"", ""$e_label"", $e_techn)");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!unquoted function Interact_U($e_from, $e_to, $e_label) ");
                        writer.WriteLine(@"  Interact2_U($index, ""$e_from"", ""$e_to"", ""$e_label"")");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_U($e_from, $e_to, $e_label, $e_techn) ");
                        writer.WriteLine(@"  Interact2_U($index, ""$e_from"", ""$e_to"", ""$e_label"", $e_techn)");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_Up($e_from, $e_to, $e_label) ");
                        writer.WriteLine(@"  Interact2_Up($index, ""$e_from"", ""$e_to"", ""$e_label"")");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_Up($e_from, $e_to, $e_label, $e_techn) ");
                        writer.WriteLine(@"  Interact2_Up($index, ""$e_from"", ""$e_to"", ""$e_label"", $e_techn)");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!unquoted function Interact_L($e_from, $e_to, $e_label) ");
                        writer.WriteLine(@"  Interact2_L($index, ""$e_from"", ""$e_to"", ""$e_label"")");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_L($e_from, $e_to, $e_label, $e_techn) ");
                        writer.WriteLine(@"  Interact2_L($index, ""$e_from"", ""$e_to"", ""$e_label"", $e_techn)");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_Left($e_from, $e_to, $e_label) ");
                        writer.WriteLine(@"  Interact2_Left($index, ""$e_from"", ""$e_to"", ""$e_label"")");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_Left($e_from, $e_to, $e_label, $e_techn) ");
                        writer.WriteLine(@"  Interact2_Left($index, ""$e_from"", ""$e_to"", ""$e_label"", $e_techn)");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!unquoted function Interact_R($e_from, $e_to, $e_label) ");
                        writer.WriteLine(@"  Interact2_R($index, ""$e_from"", ""$e_to"", ""$e_label"")");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_R($e_from, $e_to, $e_label, $e_techn) ");
                        writer.WriteLine(@"  Interact2_R($index, ""$e_from"", ""$e_to"", ""$e_label"", $e_techn)");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_Right($e_from, $e_to, $e_label) ");
                        writer.WriteLine(@"  Interact2_Right($index, ""$e_from"", ""$e_to"", ""$e_label"")");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                        writer.WriteLine(@"!unquoted function Interact_Right($e_from, $e_to, $e_label, $e_techn) ");
                        writer.WriteLine(@"  Interact2_Right($index, ""$e_from"", ""$e_to"", ""$e_label"", $e_techn)");
                        writer.WriteLine(@"  !$index=$inc_($index)");
                        writer.WriteLine(@"!endfunction");
                    }
                    break;

                case DeploymentView _:
                    if (!string.IsNullOrWhiteSpace(CustomC4BaseUrl))
                    {
                        writer.WriteLine($"!includeurl {CustomC4BaseUrl}C4_Deployment.puml");
                    }
                    else
                    {
                        writer.WriteLine(@"!include <C4/C4_Container>");
                        // Add missing deployment nodes (until they are part of the plantuml macros)
                        writer.WriteLine(@"' C4_Deployment.puml is missing, simulate it with following definitions");

                        writer.WriteLine(@"' Scope: A single software system.");
                        writer.WriteLine(@"' Primary elements: Deployment nodes and containers within the software system in scope.");
                        writer.WriteLine(@"' Intended audience: Technical people inside and outside of the software development team; including software architects, developers and operations/support staff.");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"' Colors");
                        writer.WriteLine(@"' ##################################");
                        writer.WriteLine(@"!define NODE_FONT_COLOR    #444444");
                        writer.WriteLine(@"!define NODE_BG_COLOR      #FFFFFF");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"' Styling");
                        writer.WriteLine(@"' ##################################");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"skinparam rectangle<<node>> {");
                        writer.WriteLine(@"  Shadowing false");
                        writer.WriteLine(@"  StereotypeFontSize 0");
                        writer.WriteLine(@"  FontColor NODE_FONT_COLOR");
                        writer.WriteLine(@"  BackgroundColor NODE_BG_COLOR");
                        writer.WriteLine(@"  BorderColor #444444");
                        writer.WriteLine(@"}");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"' Layout");
                        writer.WriteLine(@"' ##################################");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"!definelong LAYOUT_WITH_LEGEND");
                        writer.WriteLine(@"hide stereotype");
                        writer.WriteLine(@"legend right");
                        writer.WriteLine(@"|=                    |= Type                |");
                        writer.WriteLine(@"|<NODE_BG_COLOR>      | deployment node      |");
                        writer.WriteLine(@"|<CONTAINER_BG_COLOR> | deployment container |");
                        writer.WriteLine(@"endlegend");
                        writer.WriteLine(@"!enddefinelong");
                        writer.WriteLine(@"");
                        writer.WriteLine(@"' Nodes");
                        writer.WriteLine(@"' ##################################");
                        writer.WriteLine(@"' PlantUML does not support automatic line breaks of container, if e_techn is very long insert line breaks with ");
                        writer.WriteLine(@"' ""</size>\n<size:TECHN_FONT_SIZE>""");
                        writer.WriteLine(@"!define Node(e_alias, e_label, e_techn) rectangle ""==e_label\n<size:TECHN_FONT_SIZE>[e_techn]</size>"" <<node>> as e_alias");
                    }
                    break;

                default:
                    writer.WriteLine(!string.IsNullOrWhiteSpace(CustomC4BaseUrl)
                        ? $"!includeurl {CustomC4BaseUrl}C4_Container.puml"
                        : $"!include <C4/C4_Container>"); // as long no stdlib is used the Component diagram definition can be reused
                    break;
            }

            writer.WriteLine("!define ICONURL https://raw.githubusercontent.com/tupadr3/plantuml-icon-font-sprites/v2.4.0");
            writer.WriteLine("!includeurl ICONURL/common.puml");

            foreach (var element in view.Elements)
            {
                var sprite = element?.Element?.GetSprite();
                if (!string.IsNullOrWhiteSpace(sprite))
                {
                    writer.WriteLine($"!includeurl ICONURL/{sprite}.puml");
                }
            }
            writer.WriteLine();
            writer.WriteLine($"' {view.GetType()}: {view.Key}");
            writer.WriteLine("title " + GetTitle(view));
            writer.WriteLine();
            writer.WriteLine("SHOW_PERSON_OUTLINE()");

            WriteStyles(writer);

            switch (view?.AutomaticLayout?.RankDirection)
            {
                case RankDirection.LeftRight:
                    writer.WriteLine("LAYOUT_LEFT_RIGHT()");
                    break;

                case RankDirection.TopBottom:
                    writer.WriteLine("LAYOUT_TOP_DOWN()");
                    break;
                default:
                    writer.WriteLine("LAYOUT_LANDSCAPE()");
                    break;
            }


        }

        private string GetMacro(string macro, bool isDatabase, bool external)
        {
            if (isDatabase)
                macro += "Db";

            if (external)
                macro += "_Ext";

            return macro;
        }

        private string Quote(string s)
        {
            return $"\"{s}\"";
        }
        protected virtual void Write(Element element, TextWriter writer, int indentLevel = 0, bool asBoundary = false)
        {
            var indent = indentLevel == 0 ? string.Empty : new string(' ', indentLevel * 2);

            string macro;
            string technology;
            string args;

            var alias = TokenizeName(element);
            var title = element.Name;
            var description = element.Description;
            var sprite = element.GetSprite() ?? string.Empty;
            var tags = element.GetAllTags().Where(t=>t!="Element" && t!="Container").FirstOrDefault("");
            var link = element.GetLink() ?? string.Empty;
            

            if (asBoundary)
            {
                switch (element)
                {
                    case SoftwareSystem _:
                        macro = "System_Boundary";
                        args = $"{alias},{Quote(title)},{Quote(tags)},{Quote(link)}";
                        break;
                    case Container _:
                        macro = "Container_Boundary";
                        args = $"{alias},{Quote(title)},{Quote(tags)},{Quote(link)}";
                        break;
                    case DeploymentNode deploymentNode:
                        macro = "Node";

                        title = deploymentNode.Name + (deploymentNode.Instances > 1 ? $" (x{deploymentNode.Instances})" : "");
                        technology = deploymentNode.Technology;
                        // PlantUML supports no automatic line breaks of titles, if it belongs to a surrounding object
                        // make workaround with html tags (they are not working via multiple lines too)
                        if (technology?.Length > 30)
                            technology = BlockText(technology, 30, @"</size>\n<size:TECHN_FONT_SIZE>");

                        args = $"{alias},{Quote(title)},{Quote(EscapeText(technology))},{Quote(EscapeText(description))},{Quote(sprite)},{Quote(tags)},{Quote(link)}";
                        break;
                    default:
                        throw new NotSupportedException($"{element.GetType()} not supported boundary type");
                }

                writer.WriteLine($"{indent}{macro}({args}) {{");
                return;
            }


            if (element is Person p)
            {
                macro = GetMacro("Person", false, p.Location == Location.External);
                args = $"{alias},{Quote(title)},{Quote(EscapeText(description))},{Quote(sprite)},{Quote(tags)},{Quote(link)}";
            }
            else
            {
                switch (element)
                {
                    case SoftwareSystem system:
                        macro = GetMacro("System", false, system.Location == Location.External);
                        args = $"{alias},{Quote(title)},{Quote(EscapeText(description))},{Quote(sprite)},{Quote(tags)},{Quote(link)}";
                        break;
                    case Container container:
                        macro = GetMacro("Container", container.GetIsDatabase(), false);
                        technology = container.Technology;
                        args = $"{alias},{Quote(title)},{Quote(EscapeText(technology))},{Quote(EscapeText(description))},{Quote(sprite)},{Quote(tags)},{Quote(link)}";
                        break;
                    case Component component:
                        macro = GetMacro("Component", component.GetIsDatabase(), false);
                        technology = component.Technology;
                        args = $"{alias},{Quote(title)},{Quote(EscapeText(technology))},{Quote(EscapeText(description))},{Quote(sprite)},{Quote(tags)},{Quote(link)}";
                        break;
                    case ContainerInstance instance:
                        macro = GetMacro("Container", instance.Container.GetIsDatabase(), false);
                        title = instance.Container.Name;
                        description = instance.Container.Description;
                        technology = instance.Container.Technology;
                        args = $"{alias},{Quote(title)},{Quote(EscapeText(technology))},{Quote(EscapeText(description))},{Quote(sprite)},{Quote(tags)},{Quote(link)}";
                        break;

                    default:
                        throw new NotSupportedException($"Unsupported element type {element.GetType()}");
                }
            }

            writer.WriteLine($"{indent}{macro}({args})");
        }

        protected virtual void Write(ISet<RelationshipView> relationships, TextWriter writer)
        {
            relationships
                .OrderBy(rv => rv.Relationship.Source.Name + rv.Relationship.Destination.Name).ToList()
                .ForEach(rv => Write(rv, writer));
        }

        protected virtual void Write(RelationshipView relationshipView, TextWriter writer, string? advancedDescription = null)
        {
            var relationship = relationshipView.Relationship;
            string
                source = TokenizeName(relationship.Source),
                dest = TokenizeName(relationship.Destination),
                label = advancedDescription ?? relationship.Description ?? "",
                tech = relationship.Technology;

            var macro = GetSpecificLayoutMacro(relationshipView);

            writer.Write($"{macro}({source}, {dest}, \"{EscapeText(label)}\"");
            if (tech != null)
                writer.Write($", \"{EscapeText(tech)}\"");
            writer.WriteLine(")");
        }

        protected virtual void WriteDynamicInteractions(ISet<RelationshipView> relationships, TextWriter writer)
        {
            relationships
                .OrderBy(rv => rv.Order).ToList()
                .ForEach(rv => WriteDynamicInteraction(rv, writer, rv.Order, rv.Description));
        }

        protected virtual void WriteDynamicInteraction(RelationshipView relationshipView, TextWriter writer, string order, string label = null)
        {
            var relationship = relationshipView.Relationship;
            string
                source = TokenizeName(relationship.Source),
                dest = TokenizeName(relationship.Destination),
                tech = relationship.Technology;

            var macro = GetSpecificLayoutMacro(relationshipView);
            macro = "Interact2" + macro.Substring("Rel".Length);

            writer.Write($"{macro}(\"{order}\", {source}, {dest}, \"{EscapeText(label)}\"");
            if (tech != null)
                writer.Write($", \"{EscapeText(tech)}\"");
            writer.WriteLine(")");
        }

        private static string GetSpecificLayoutMacro(RelationshipView relationshipView)
        {
            var direction = relationshipView.GetDirection(out _);
            switch (direction)
            {
                case DirectionValues.Back: return "Rel_Back";
                case DirectionValues.Neighbor: return "Rel_Neighbor";  // C4 PlantUml without u
                case DirectionValues.Neighbour: return "Rel_Neighbor";  // C4 PlantUml without u
                case DirectionValues.BackNeighbor: return "Rel_Back_Neighbor"; // C4 PlantUml without u
                case DirectionValues.BackNeighbour: return "Rel_Back_Neighbor"; // C4 PlantUml without u
                case DirectionValues.Up: return "Rel_Up";
                case DirectionValues.Down: return "Rel_Down";
                case DirectionValues.Left: return "Rel_Left";
                case DirectionValues.Right: return "Rel_Right";
                default: return "Rel";
            }
        }


        private void WriteStyles(TextWriter writer)
        {
            var styles = Workspace.Views.Configuration.Styles;
            
            foreach (var es in styles.Elements)
            {
                var fontColor = ValueOrDefault(es.Color, "$ELEMENT_FONT_COLOR");
                if (!fontColor.StartsWith($"")) fontColor = Quote(fontColor);
                var bgColor = ValueOrDefault(es.Background, "$CONTAINER_BG_COLOR");
                if (!bgColor.StartsWith($"")) bgColor= Quote(bgColor);
                var shape = "RoundedBoxShare()";
                switch (es.Shape)
                {
                    case Shape.Diamond: 
                        shape = "EightSidedShape()";
                        break;
                }

                var legend = es.Description;
                writer.WriteLine($"AddElementTag({Quote(es.Tag)}, $fontColor={fontColor}, $bgColor={bgColor}, $shape={shape}, $legendText={legend})");
            }

            foreach (var rs in styles.Relationships)
            {
                var textColor = ValueOrDefault(rs.Color, "$ARROW_COLOR");
                if (!textColor.StartsWith($"")) textColor = Quote(textColor);
                var lineColor = ValueOrDefault(rs.Color, "$ARROW_COLOR");
                if (!lineColor.StartsWith($"")) lineColor = Quote(lineColor);
                var lineStyle = "";
                if (rs.Dashed.GetValueOrDefault()) lineStyle = "DashedLine()";
                
                writer.WriteLine($"AddRelTag({Quote(rs.Tag)}, $textColor={textColor}, $lineColor={lineColor}, $lineStyle={lineStyle})");
            }
       }

    }
}