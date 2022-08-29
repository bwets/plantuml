using System.Text;
using Structurizr;

namespace bwets.PlantUML.Structurizr
{
    public abstract class PlantUMLWriterBase : IPlantUMLWriter, IDisposable
    {
        protected readonly Workspace Workspace;
        private readonly List<Stream> _streams = new List<Stream>();

        public bool ShowLegend { get; set; } = true;

        public void Close()
        {
            foreach (var stream in _streams)
            {
                stream.Dispose();
            }
        }
        
        protected PlantUMLWriterBase(Workspace workspace)
        {
            Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        }

        public void Write(string path)
        {
            if (!Directory.Exists(path)) throw new ArgumentNullException(nameof(path), "Path does not exist");

            Workspace.Views.SystemLandscapeViews.ToList().ForEach(v => Write(v, path));
            Workspace.Views.SystemContextViews.ToList().ForEach(v => Write(v, path));
            Workspace.Views.ContainerViews.ToList().ForEach(v => Write(v, path));
            Workspace.Views.ComponentViews.ToList().ForEach(v => Write(v, path));
            Workspace.Views.DynamicViews.ToList().ForEach(v => Write(v, path));
            Workspace.Views.DeploymentViews.ToList().ForEach(v => Write(v, path));
        }


        protected TextWriter GetWriter(View view, string type, string path)
        {
            var filename = Path.Combine(path, $"{type}.{view.Key}.puml");
            if (File.Exists(filename)) File.Delete(filename);
            var stream = File.OpenWrite(filename);
            _streams.Add(stream);
            var writer = new StreamWriter(stream, Encoding.UTF8);
            return writer;
        }

        public void Write(View view, string path)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

       
            switch (view)
            {
                case SystemLandscapeView sl:
                    Write(sl, path);
                    break;
                case SystemContextView sc:
                    Write(sc, path);
                    break;
                case ContainerView ct:
                    Write(ct, path);
                    break;
                case ComponentView cp:
                    Write(cp, path);
                    break;
                case DynamicView dy:
                    Write(dy, path);
                    break;
                case DeploymentView de:
                    Write(de, path);
                    break;
                default:
                    throw new NotSupportedException($"{view.GetType()} not supported for export");
            }
        }

        protected abstract void Write(SystemLandscapeView view, string path);
        protected abstract void Write(SystemContextView view, string path);
        protected abstract void Write(ContainerView view, string path);
        protected abstract void Write(ComponentView view, string path);
        protected abstract void Write(DynamicView view, string path);
        protected abstract void Write(DeploymentView view, string path);

        protected virtual void WriteProlog(View view, TextWriter writer)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.WriteLine("@startuml");
            writer.WriteLine("' key: " + view.Key);
            writer.WriteLine("title " + GetTitle(view));
        }

     
        protected virtual void WriteEpilog(View view, TextWriter writer)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            
            if(ShowLegend)  writer.WriteLine("SHOW_LEGEND()");
            
            writer.WriteLine("@enduml");
            writer.WriteLine("");
        }

 
        protected string TokenizeName(Element e) =>
            e != null
                ? TokenizeName(e.CanonicalName, e.GetHashCode())
                : throw new ArgumentNullException(nameof(e));
        
        protected string TokenizeName(string s, int? hash = null)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";

            // canonically name calculation changed
            // a) instead of "/" starts with "{ElementType}://"; remove it that it is compatible with old impl.
            // b) deployment namespaces are added with "/"; remove it that it is shorter (unique parts created via hash)
            // c) orig "/" in static namespaces replaced with "."; replace with "__" that it is compatible with old impl.
            var p = s.LastIndexOf('/');
            if (p >= 0)
                s = s.Substring(p + 1);

            s = s.Replace(" ", string.Empty)
                 .Replace("-", string.Empty)
                 .Replace("[", string.Empty)
                 .Replace("]", string.Empty)
                 .Replace(".", "__");

            if (hash.HasValue)
            {
                s = s + "__" + hash.Value.ToString("x");
            }

            return s;
        }

     
        protected virtual string GetTitle(View view) =>
            view != null
                ? string.IsNullOrWhiteSpace(view.Title) ? view.Name : view.Title
                : throw new ArgumentNullException(nameof(view));

        protected string BlockText(string s, int blockWidth, string formattedLineBreak)
        {
            var block = s;

            if (blockWidth <= 0 || s.Contains("\n") || s.Contains("\r")) return block;

            var formatted = new StringBuilder();
            var pos = 0;
            var word = string.Empty;

            foreach (var c in s)
            {
                word += c;
                if (c != ' ') continue;
                if (pos != 0 && pos + word.Length > blockWidth)
                {
                    formatted.Append(formattedLineBreak);
                    pos = 0;
                }
                formatted.Append(word);
                pos += word.Length;
                word = "";
            }

            if (word.Length > 0)
            {
                if (pos != 0 && pos + word.Length > blockWidth)
                    formatted.Append(formattedLineBreak);
                formatted.Append(word);
            }

            block = formatted.ToString();

            return block;
        }

        protected string EscapeText(string s) => string.IsNullOrWhiteSpace(s) ? "" : s.Replace("\"", "&quot;");


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PlantUMLWriterBase()
        {
            Dispose(false);
        }
    }
}