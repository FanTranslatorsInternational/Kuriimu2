using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kontract.Models.Text
{
    public class ProcessedText
    {
        private const char EscapeChar_ = '\\';
        private const char ControlCodeOpen_ = '{';
        private const char ControlCodeClose_ = '}';
        private static readonly char[] EscapeableChars = { '{' };

        // TODO: Give access to those elements for processing in other parts of a text processing pipeline
        private readonly ProcessedElement[] _processedElements;

        public ProcessedText(string text)
        {
            _processedElements = new ProcessedElement[] { text };
        }

        public ProcessedText(ProcessedElement[] elements)
        {
            _processedElements = elements;
        }

        public string Serialize(bool withControlCodes = true)
        {
            return _processedElements
                .Where(x => x.IsString || withControlCodes && x.IsControlCode)
                .Aggregate("", (a, b) => a + b);
        }

        /// <summary>
        /// Parses a given text into processed elements.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The processed text instance.</returns>
        public static ProcessedText Parse(string text)
        {
            var elements = new List<ProcessedElement>();

            var result = string.Empty;
            for (var i = 0; i < text.Length; i++)
            {
                if (IsControlCode(text, i, out var controlCodeEnd))
                {
                    if (result != string.Empty)
                    {
                        elements.Add(result);
                        result = string.Empty;
                    }

                    elements.Add(ControlCode.Parse(text[i..controlCodeEnd]));
                    i += controlCodeEnd - i;

                    continue;
                }

                if (IsEscapedChar(text, i))
                    i++;

                result += text[i];
            }

            return new ProcessedText(elements.ToArray());
        }

        private static bool IsControlCode(string text, int index, out int controlCodeEnd)
        {
            controlCodeEnd = -1;

            // Normally, this should never produce a false positive with escapeable characters,
            // since the check would encounter the escape character first, instead of the { for the control code start
            if (text[index] != ControlCodeOpen_)
                return false;

            // It is not allowed to have a } inside a control code, therefore we may not assume
            // an escaped character inside a control code structure
            for (var i = index + 1; i < text.Length; i++)
                if (text[i] == ControlCodeClose_)
                {
                    controlCodeEnd = i + 1;
                    return true;
                }

            return false;
        }

        private static bool IsEscapedChar(string text, int index)
        {
            var result = text[index] == EscapeChar_ && index + 1 < text.Length;
            return result && EscapeableChars.Any(c => text[index + 1] == c);
        }
    }

    public class ProcessedElement
    {
        private string _text;
        private ControlCode _code;

        public bool IsString { get; }

        public bool IsControlCode { get; }

        public ProcessedElement(string text)
        {
            _text = text;
            IsString = true;
        }

        public ProcessedElement(ControlCode code)
        {
            _code = code;
            IsControlCode = true;
        }

        public string GetText()
        {
            if (IsControlCode)
                throw new InvalidOperationException("Element does not contain a text.");

            return _text;
        }

        public ControlCode GetControlCode()
        {
            if (IsString)
                throw new InvalidOperationException("Element does not contain a control code.");

            return _code;
        }

        public override string ToString()
        {
            return IsString ? _text : _code.ToString();
        }

        public static implicit operator ProcessedElement(string text) => new ProcessedElement(text);
        public static implicit operator ProcessedElement(ControlCode code) => new ProcessedElement(code);

        public static explicit operator ControlCode(ProcessedElement e) => e.GetControlCode();
        public static explicit operator string(ProcessedElement e) => e.GetText();
    }

    public class ControlCode
    {
        public int Id { get; }

        public string Name { get; }

        public string[] Arguments { get; }

        public ControlCode(int id, params string[] args)
        {
            Id = id;
            Arguments = args;
        }

        public ControlCode(string name, params string[] args)
        {
            Name = name;
            Arguments = args;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("{");
            sb.Append(Name ?? Id.ToString());

            if (Arguments.Length > 0)
                sb.AppendJoin(' ', Arguments);

            sb.Append("}");

            return sb.ToString();
        }

        /// <summary>
        /// Tries to parse a text containing a single control code in the format '{xxx/nnn arg1 arg2}'.
        /// With xxx being the Id, nnn the name, and argX being a collection of space separated arguments.
        /// xxx and nnn are interchangeable and only one of the two may be given at once.
        /// </summary>
        /// <param name="code">The control code to parse.</param>
        /// <param name="result">The result of the parsing.</param>
        /// <returns>The parsed control code.</returns>
        public static bool TryParse(string code, out ControlCode result)
        {
            result = null;

            try
            {
                result = Parse(code);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses a text containing a single control code in the format '{xxx/nnn arg1 arg2}'.
        /// With xxx being the Id, nnn the name, and argX being a collection of space separated arguments.
        /// xxx and nnn are interchangeable and only one of the two may be given at once.
        /// </summary>
        /// <param name="code">The control code to parse.</param>
        /// <returns>The parsed control code.</returns>
        public static ControlCode Parse(string code)
        {
            if (!code.StartsWith('{') || !code.EndsWith('}'))
                throw new InvalidOperationException("The code is not encased in { and }.");

            var parameter = code[1..^1].Split(' ');

            if (int.TryParse(parameter[0], out var id))
                return new ControlCode(id, parameter[1..]);

            return new ControlCode(parameter[0], parameter[1..]);
        }
    }
}
