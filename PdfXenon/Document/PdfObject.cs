﻿using System;
using System.Text;

namespace PdfXenon.Standard
{
    public abstract class PdfObject
    {
        public PdfObject(PdfObject parent)
            : this(parent, null)
        {
        }

        public PdfObject(PdfObject parent, ParseObject parse)
        {
            Parent = parent;
            ParseObject = parse;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Output(sb, 0);
            return sb.ToString();
        }

        public virtual int Output(StringBuilder sb, int indent)
        {
            string output = $"({GetType().Name})";
            sb.Append(output);
            return indent + output.Length;
        }

        public ParseObject ParseObject { get; private set; }
        public PdfObject Parent { get; private set; }
        public PdfDocument Document { get => TypedParent<PdfDocument>(); }
        public PdfDecrypt Decrypt { get => TypedParent<PdfDocument>().DecryptHandler; }

        public T TypedParent<T>() where T : PdfObject
        {
            PdfObject parent = Parent;

            while (parent != null)
            {
                if (parent is T)
                    return parent as T;
                else
                    parent = parent.Parent;
            }

            return null;
        }

        public PdfObject WrapObject(ParseObject obj)
        {
            if (obj is ParseString)
                return new PdfString(this, obj as ParseString);
            if (obj is ParseName)
                return new PdfName(this, obj as ParseName);
            else if (obj is ParseInteger)
                return new PdfInteger(this, obj as ParseInteger);
            else if (obj is ParseReal)
                return new PdfReal(this, obj as ParseReal);
            else if (obj is ParseDictionary)
                return new PdfDictionary(this, obj as ParseDictionary);
            else if (obj is ParseObjectReference)
                return new PdfObjectReference(this, obj as ParseObjectReference);
            else if (obj is ParseStream)
                return new PdfStream(this, obj as ParseStream);
            else if (obj is ParseArray)
                return new PdfArray(this, obj as ParseArray);
            else if (obj is ParseIdentifier)
                return new PdfIdentifier(this, obj as ParseIdentifier);
            else if (obj is ParseBoolean)
                return new PdfBoolean(this, obj as ParseBoolean);

            throw new ApplicationException($"Cannot wrap object '{obj.GetType().Name}' as a pdf object .");
        }
    }
}
