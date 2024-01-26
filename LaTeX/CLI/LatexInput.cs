using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace PrimerTools.Latex
{
    public sealed record LatexInput(string code, List<string> headers) : ISerializable
    {
        public static LatexInput From(string code) => new(code, GetDefaultHeaders());

        public int GetDeterministicHashCode()
        {
            return $"{code}_${string.Join(',', headers)}".GetDeterministicHashCode();
        }

        public bool Equals(LatexInput other)
        {
            return other != null
                && code == other.code
                // We compare them with == because we want to know if both of them are null
                // or both o them are the same object, in both cases we don't compare their content
                // ReSharper disable once PossibleUnintendedReferenceComparison
                && (headers == other.headers || headers.SequenceEqual(other.headers));
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(code);

            for (var i = 0; i < headers.Count; i++)
                hash.Add(headers[i]);

            return hash.ToHashCode();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(code), code);
            info.AddValue(nameof(headers), headers);
        }

        public static List<string> GetDefaultHeaders() => new() {
            @"\documentclass[preview]{standalone}",
            @"\usepackage[english]{babel}",
            @"\usepackage[utf8]{inputenc}",
            @"\usepackage[T1]{fontenc}",
            @"\usepackage{amsmath}",
            @"\usepackage{amssymb}",
            @"\usepackage{dsfont}",
            @"\usepackage{setspace}",
            @"\usepackage{tipa}",
            @"\usepackage{relsize}",
            @"\usepackage{textcomp}",
            @"\usepackage{mathrsfs}",
            @"\usepackage{calligra}",
            @"\usepackage{wasysym}",
            @"\usepackage{ragged2e}",
            @"\usepackage{physics}",
            @"\usepackage{xcolor}",
            @"\usepackage{microtype}",
            @"\usepackage{pifont}",
            @"\linespread{1}",
            @"\usepackage{concmath-otf}",
            @"\usepackage{enumitem}"
        };
    }
}
