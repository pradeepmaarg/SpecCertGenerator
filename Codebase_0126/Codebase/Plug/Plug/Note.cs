using System;
namespace Maarg.Fatpipe.Plug.DataModel
{
    public enum NoteType
    {
        Syntax,
        Semantic,
        Comment,
    }

    public class Note
    {
        public NoteType Type { get; set; }
        public string Description { get; set; }

        public Note(string type, string description)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentNullException("type", "Note type cannot be null");
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentNullException("description", "Note description cannot be null");

            switch (type.ToLower())
            {
                case "syntax": Type = NoteType.Syntax; break;
                case "semantic": Type = NoteType.Semantic; break;
                case "comment": Type = NoteType.Comment; break;
                default:
                    throw new ArgumentException(string.Format("Invalid note type '{0}'", type), "type");
            }

            Description = description;
        }
    }
}
