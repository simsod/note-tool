using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Standard;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Store;
using Lucene.Net.Util;
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
            var fieldType = new FieldType(TextField.TYPE_STORED) {
                StoreTermVectors = true,
                StoreTermVectorOffsets = true
            }.Freeze();
            
            var doc = new Document {
                new TextField("content", source.Content, Field.Store.YES),
                new Field("content-tv", source.Content,fieldType),
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
        public string Content { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public float Score { get; set; }
        public int LineNumber { get; set; }
    }

    public NoteDocument[] Search(string searchQuery, int pageSize = 20) {
        using var writer = GetWriter();
        
        var queryParserHelper = new StandardQueryParser();
        var query = queryParserHelper.Parse(searchQuery, "content");

        using var reader = writer.GetReader(applyAllDeletes: true);
        
        var searcher = new IndexSearcher(reader);
        var hits = searcher.Search(query, pageSize).ScoreDocs;
        var htmlFormatter = new SimpleHTMLFormatter("[bold yellow]", "[/]");
        var highlighter = new Highlighter(htmlFormatter, new QueryScorer(query));
        var analyzer = new StandardAnalyzer(AppLuceneversion);
        
        var result = new List<NoteDocument>();
        foreach (var hit in hits) {
            var id = hit.Doc;
            var foundDoc = searcher.Doc(hit.Doc);
            var content = foundDoc.Get("content");
            var tokenStream = TokenSources.GetAnyTokenStream(searcher.IndexReader, id, "content",analyzer);
            var fragments = highlighter.GetBestTextFragments(tokenStream, content, mergeContiguousFragments: true,maxNumFragments: 1);

            var frags = fragments.Select(x => x.ToString()).ToArray();
            var highlighted = string.Join("...", frags);
            
            
            var doc = new NoteDocument {
                Score = hit.Score,
                FileName = foundDoc.Get("filename"),
                Created = DateTime.ParseExact(foundDoc.Get("created"), "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                Modified = DateTime.ParseExact(foundDoc.Get("modified"), "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                Content = highlighted
            };
            result.Add(doc);
        }

        return result.ToArray();
    }
}