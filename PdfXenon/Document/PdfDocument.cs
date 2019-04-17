﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdfXenon.Standard
{
    public class PdfDocument
    {
        private bool _open;
        private Stream _stream;
        private StreamReader _reader;
        private Parser _parser;
        private ParseObjectReference _refCatalog;
        private ParseObjectReference _refInfo;
        private PdfCatalog _pdfCatalog;
        private PdfInfo _pdfInfo;

        public PdfDocument()
        {
            Version = new PdfVersion(this, 0, 0);
            IndirectObjects = new PdfIndirectObjects(this);
            Decrypt = new PdfDecryptNone(this);
        }

        public PdfVersion Version { get; private set; }
        public PdfIndirectObjects IndirectObjects { get; private set; }
        public PdfDecrypt Decrypt { get; private set; }

        public void Load(string filename, bool immediate = false)
        {
            if (_open)
                throw new ApplicationException("Document already has a stream open.");

            _reader = new StreamReader(filename);
            Load(_reader.BaseStream, immediate);
        }

        public void Load(Stream stream, bool immediate = false)
        {
            if (_open)
                throw new ApplicationException("Document already has a stream open.");

            _stream = stream;
            _parser = new Parser(_stream);
            _parser.ResolveReference += Parser_ResolveReference;

            // PDF file should have a well known marker at top of file
            _parser.ParseHeader(out int versionMajor, out int versionMinor);
            Version = new PdfVersion(this, versionMajor, versionMinor);

            // Find stream position of the last cross-reference table
            long xRefPosition = _parser.ParseXRefOffset();
            bool lastHeader = true;

            do
            {
                // Get the aggregated set of entries from all the cross-reference table sections
                List<TokenXRefEntry> xrefs = _parser.ParseXRef(xRefPosition);

                // Should always be positioned at the trailer after parsing cross-table references
                ParseDictionary trailer = _parser.ParseTrailer();
                ParseInteger size = trailer.MandatoryValue<ParseInteger>("Size");
                foreach (TokenXRefEntry xref in xrefs)
                {
                    // Ignore unused entries and entries smaller than the defined size from the trailer dictionary
                    if (xref.Used && (xref.Id < size.Value))
                        IndirectObjects.AddXRef(xref);
                }

                if (lastHeader)
                {
                    // Replace the default decryption handler with one from document settings
                    Decrypt = PdfDecrypt.CreateDecrypt(this, trailer);

                    // We only care about the latest defined catalog and information dictionary
                    _refCatalog = trailer.MandatoryValue<ParseObjectReference>("Root");
                    _refInfo = trailer.OptionalValue<ParseObjectReference>("Info");
                }

                // If there is a previous cross-reference table, then we want to process that as well
                ParseInteger prev = trailer.OptionalValue<ParseInteger>("Prev");
                if (prev != null)
                    xRefPosition = prev.Value;
                else
                    xRefPosition = 0;

                lastHeader = false;

            } while (xRefPosition > 0);

            _open = true;

            if (immediate)
            {
                // Must load all objects immediately so the stream can then be closed
                foreach (var id in IndirectObjects.Values)
                {
                    foreach (var gen in id.Values)
                        ResolveReference(gen.Id, gen.Gen);
                }

                Close();
            }
        }

        public void Close()
        {
            if (_open)
            {
                if (_reader != null)
                {
                    _reader.Dispose();
                    _reader = null;
                }

                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }

                if (_parser != null)
                {
                    _parser.Dispose();
                    _parser = null;
                }

                _open = false;
            }
        }

        public PdfCatalog Catalog
        {
            get
            {
                if (_pdfCatalog == null)
                    _pdfCatalog = new PdfCatalog(this, IndirectObjects.MandatoryValue<ParseDictionary>(_refCatalog));

                return _pdfCatalog;
            }
        }

        public PdfInfo Info
        {
            get
            {
                if ((_pdfInfo == null) && (_refInfo != null))
                    _pdfInfo = new PdfInfo(this, IndirectObjects.MandatoryValue<ParseDictionary>(_refInfo));

                return _pdfInfo;
            }
        }

        public ParseObject ResolveReference(ParseObjectReference reference)
        {
            return ResolveReference(reference.Id, reference.Gen);
        }

        public ParseObject ResolveReference(int id, int gen)
        {
            PdfIndirectObjectGen indirect = IndirectObjects[id, gen];
            if (indirect != null)
            {
                if (indirect.Child == null)
                {
                    ParseIndirectObject parseIndirectObject = _parser.ParseIndirectObject(indirect.Offset);
                    indirect.Child = parseIndirectObject.Object;

                    //Console.WriteLine(parseIndirectObject.ToString());
                }

                return indirect.Child;
            }

            return null;
        }

        private void Parser_ResolveReference(object sender, ParseResolveEventArgs e)
        {
            e.Object = ResolveReference(e.Id, e.Gen);
        }
    }
}
