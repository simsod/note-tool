using CommandLine;

namespace NoteTool {
    [Verb("configure", HelpText = "Configure the note-tool")]
    class ConfigureOptions {
        [Option] public string? Path { get; set; }
        [Option] public string? TemplatesPath { get; set; }
        [Option] public bool WriteTemplates { get; set; }
        [Option] public string? OpenWith { get; set; }
    }
    
    [Verb("standup", HelpText = "Add a new standup note")]
    class StandupOptions {
        //normal options here
    }

    [Verb("meeting", HelpText = "Add a new meeting note")]
    class MeetingOptions {
        [Value(0)] public string? Topic { get; set; }
    }

    [Verb("note", HelpText = "Add a new note")]
    class NoteOptions {
        [Value(0)]public string? Topic { get; set; }
    }

    [Verb("search", HelpText = "Searches the notes folder")]
    class SearchNotesOption {
        [Value(0)]public string? Query { get; set; }
        [Option('c',"count", HelpText = "The maximum number of items to display", Default = 20)] public int Count { get; set; } = 20;
    }

    [Verb("index", HelpText = "Indexes all notes in the folder")]
    class SearchIndexOption {
        
    }


    [Verb("new", HelpText = "Created a new note of the specified type")]
    class NewOptions {
        
    }
    
}