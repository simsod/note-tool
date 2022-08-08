using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NoteTool.Extensions;
using Directory = System.IO.Directory;


namespace NoteTool.Services;

public class SearchService {
    private readonly ToolConfiguration _config;
    private const LuceneVersion AppLuceneversion = LuceneVersion.LUCENE_48;

    public SearchService(ToolConfiguration config) {
        _config = config;
    }

    private IndexWriter GetWriter() {
        var analyzer = new StandardAnalyzer(AppLuceneversion);
        var indexConfig = new IndexWriterConfig(AppLuceneversion, analyzer);

        return new IndexWriter(FSDirectory.Open(_config.IndexPath), indexConfig);
    }

    public int Index(bool deleteIndex = true) {
        if (deleteIndex) {
            if (Directory.Exists(_config.IndexPath))
                Directory.Delete(_config.IndexPath, true);

            Directory.CreateDirectory(_config.IndexPath);
        }

        using var writer = GetWriter();
        var dir = new DirectoryInfo(_config.Path);
        var files = dir.GetFiles("*.md");
        int count = 0;
        foreach (var file in files) {
            var source = new NoteDocument() {
                Created = file.CreationTime,
                Modified = file.LastWriteTime,
                Content = File.ReadAllText(file.FullName),
                FileName = file.Name
            };

            // var createdField = new NumericDocValuesField("created", source.Created.Ticks);
            // var modifiedField = new NumericDocValuesField("modified",source.Modified.Ticks);
            // createdField.FieldType.IsStored = true;
            // modifiedField.FieldType.IsStored = true;
            var doc = new Document {
                new TextField("content", source.Content, Field.Store.YES),
                new StringField("filename", source.FileName, Field.Store.YES),
                new StringField("created", source.Created.ToString("yyyyMMddHHmmss"), Field.Store.YES),
                new StringField("modified", source.Modified.ToString("yyyyMMddHHmmss"), Field.Store.YES),
            };
            writer.AddDocument(doc);
            count++;
        }

        writer.Flush(triggerMerge: false, applyAllDeletes: false);
        return count;
    }


    public class NoteDocument {
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string Content { get; set; }
        public string FileName { get; set; }
        public float Score { get; set; }
        public int LineNumber { get; set; }
    }

    public NoteDocument[] Search(string searchQuery, int pageSize = 20) {
        using var writer = GetWriter();

        var query = new PhraseQuery() {
            new Term("content", searchQuery),
        };
        using var reader = writer.GetReader(applyAllDeletes: true);
        var searcher = new IndexSearcher(reader);
        var hits = searcher.Search(query, pageSize).ScoreDocs;

        var result = new List<NoteDocument>();
        foreach (var hit in hits) {
            var foundDoc = searcher.Doc(hit.Doc);
            var doc = new NoteDocument {
                Score = hit.Score,
                FileName = foundDoc.Get("filename"),
                Created = DateTime.ParseExact(foundDoc.Get("created"), "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                Modified = DateTime.ParseExact(foundDoc.Get("modified"), "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            };

            var idx = 1;
            foreach (var line in File.ReadLines(Path.Join(_config.Path, foundDoc.Get("filename")))) {
                var hitIndex = line.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase);
                if (hitIndex != -1) {
                    var start = hitIndex - 20;
                    if (start < 0)
                        start = 0;
                    
                    var text = line.Trim().SubstringSafe(start, searchQuery.Length + 100);
                    doc.Content = text;
                    doc.LineNumber = idx; 
                    
                    break;
                }

                idx++;
            }

            result.Add(doc);
        }

        return result.ToArray();
    }
}