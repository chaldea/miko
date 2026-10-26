using static Miko.Text.Bidi.BidiClass;

namespace Miko.Text.Bidi;

internal sealed partial class BidiParagraph
{
    /// <summary>
    /// One isolating run sequence (BD13): the characters W1–I2 see as contiguous, with the
    /// characters removed by X9 already left out.
    /// </summary>
    private sealed class IsolatingRunSequence
    {
        /// <summary>Capacity of the BD16 bracket stack.</summary>
        private const int MaxBracketDepth = 63;

        private readonly BidiParagraph _paragraph;
        private readonly int[] _indexes;
        private readonly BidiClass[] _types;
        private readonly byte _level;
        private readonly BidiClass _sos;
        private readonly BidiClass _eos;

        public IsolatingRunSequence(BidiParagraph paragraph, int[] indexes)
        {
            _paragraph = paragraph;
            _indexes = indexes;
            _types = new BidiClass[indexes.Length];
            for (int i = 0; i < indexes.Length; i++)
            {
                _types[i] = paragraph._resultTypes[indexes[i]];
            }

            _level = paragraph._levels[indexes[0]];

            int previous = indexes[0] - 1;
            while (previous >= 0 && IsRemovedByX9(paragraph._initialTypes[previous])) previous--;
            int previousLevel = previous >= 0 ? paragraph._levels[previous] : paragraph._paragraphLevel;
            _sos = TypeForLevel(Math.Max(previousLevel, _level));

            int last = indexes[^1];
            int nextLevel;
            if (paragraph._initialTypes[last] is LRI or RLI or FSI)
            {
                // An unmatched isolate initiator: the sequence ends at the paragraph's end.
                nextLevel = paragraph._paragraphLevel;
            }
            else
            {
                int next = last + 1;
                while (next < paragraph._length && IsRemovedByX9(paragraph._initialTypes[next])) next++;
                nextLevel = next < paragraph._length ? paragraph._levels[next] : paragraph._paragraphLevel;
            }
            _eos = TypeForLevel(Math.Max(nextLevel, _level));
        }

        public void Resolve()
        {
            ResolveWeakTypes();
            ResolvePairedBrackets();
            ResolveNeutralTypes();
            ResolveImplicitLevels();
        }

        private void ResolveWeakTypes()
        {
            // W1: a nonspacing mark takes the type of the character before it (ON after an isolate
            // initiator or PDI, sos at the start).
            var preceding = _sos;
            for (int i = 0; i < _types.Length; i++)
            {
                var t = _types[i];
                if (t == NSM)
                {
                    _types[i] = preceding;
                }
                else
                {
                    preceding = t is LRI or RLI or FSI or PDI ? ON : t;
                }
            }

            // W2: European numbers after Arabic letters are Arabic numbers.
            var lastStrong = _sos;
            for (int i = 0; i < _types.Length; i++)
            {
                var t = _types[i];
                if (t is L or R or AL)
                {
                    lastStrong = t;
                }
                else if (t == EN && lastStrong == AL)
                {
                    _types[i] = AN;
                }
            }

            // W3.
            for (int i = 0; i < _types.Length; i++)
            {
                if (_types[i] == AL) _types[i] = R;
            }

            // W4: a single separator between two numbers of the same kind joins them.
            for (int i = 1; i < _types.Length - 1; i++)
            {
                var t = _types[i];
                var before = _types[i - 1];
                var after = _types[i + 1];
                if (t == ES && before == EN && after == EN)
                {
                    _types[i] = EN;
                }
                else if (t == CS && before == EN && after == EN)
                {
                    _types[i] = EN;
                }
                else if (t == CS && before == AN && after == AN)
                {
                    _types[i] = AN;
                }
            }

            // W5: terminators next to European numbers become European numbers.
            for (int i = 0; i < _types.Length; i++)
            {
                if (_types[i] != ET) continue;
                int limit = i + 1;
                while (limit < _types.Length && _types[limit] == ET) limit++;
                var before = i == 0 ? _sos : _types[i - 1];
                var after = limit == _types.Length ? _eos : _types[limit];
                if (before == EN || after == EN)
                {
                    _types.AsSpan(i, limit - i).Fill(EN);
                }
                i = limit;
            }

            // W6: remaining separators and terminators are neutral.
            for (int i = 0; i < _types.Length; i++)
            {
                if (_types[i] is ES or ET or CS) _types[i] = ON;
            }

            // W7: European numbers in left-to-right context are L.
            lastStrong = _sos;
            for (int i = 0; i < _types.Length; i++)
            {
                var t = _types[i];
                if (t is L or R)
                {
                    lastStrong = t;
                }
                else if (t == EN && lastStrong == L)
                {
                    _types[i] = L;
                }
            }
        }

        /// <summary>N0: paired brackets take the direction of what they enclose or surround.</summary>
        private void ResolvePairedBrackets()
        {
            if (_paragraph._bracketTypes == null) return;

            var pairs = LocateBracketPairs();
            if (pairs.Count == 0) return;
            pairs.Sort((a, b) => a.Open.CompareTo(b.Open));

            var embedding = TypeForLevel(_level);
            foreach (var (open, close) in pairs)
            {
                var inside = ClassifyPairContent(open, close, embedding);
                if (inside == ON) continue;

                var resolved = inside;
                if (inside != embedding)
                {
                    // N0 c: the opposite direction inside wins only if it also precedes the pair.
                    var before = ClassBeforePair(open);
                    resolved = before == inside ? inside : embedding;
                }
                SetBracketType(open, resolved);
                SetBracketType(close, resolved);
            }
        }

        /// <summary>BD16. Positions are indexes into this sequence.</summary>
        private List<(int Open, int Close)> LocateBracketPairs()
        {
            var pairs = new List<(int, int)>();
            var openers = new List<(int Key, int Position)>();
            var bracketTypes = _paragraph._bracketTypes!;
            var bracketKeys = _paragraph._bracketKeys!;

            for (int i = 0; i < _indexes.Length; i++)
            {
                // A bracket whose class an override changed (or that is not ON) does not pair.
                if (_types[i] != ON) continue;
                int index = _indexes[i];
                byte kind = bracketTypes[index];
                if (kind == 1)
                {
                    if (openers.Count == MaxBracketDepth)
                    {
                        // Stack overflow: stop processing this sequence (BD16).
                        return new List<(int, int)>();
                    }
                    openers.Add((bracketKeys[index], i));
                }
                else if (kind == 2)
                {
                    for (int s = openers.Count - 1; s >= 0; s--)
                    {
                        if (openers[s].Key != bracketKeys[index]) continue;
                        pairs.Add((openers[s].Position, i));
                        openers.RemoveRange(s, openers.Count - s);
                        break;
                    }
                }
            }
            return pairs;
        }

        /// <summary>
        /// The embedding direction if a strong type matching it is inside the pair, otherwise the
        /// opposite direction if one is inside, otherwise ON.
        /// </summary>
        private BidiClass ClassifyPairContent(int open, int close, BidiClass embedding)
        {
            var opposite = ON;
            for (int i = open + 1; i < close; i++)
            {
                var strong = StrongTypeForN0(_types[i]);
                if (strong == ON) continue;
                if (strong == embedding) return embedding;
                opposite = strong;
            }
            return opposite;
        }

        private BidiClass ClassBeforePair(int open)
        {
            for (int i = open - 1; i >= 0; i--)
            {
                var strong = StrongTypeForN0(_types[i]);
                if (strong != ON) return strong;
            }
            return _sos;
        }

        private static BidiClass StrongTypeForN0(BidiClass type) => type switch
        {
            L => L,
            R or AL or EN or AN => R,
            _ => ON,
        };

        /// <summary>
        /// Gives a bracket its resolved type, and also any nonspacing marks that originally followed
        /// it (W1 had given them the bracket's earlier type, ON).
        /// </summary>
        private void SetBracketType(int position, BidiClass type)
        {
            _types[position] = type;
            var initialTypes = _paragraph._initialTypes;
            for (int i = position + 1; i < _indexes.Length && initialTypes[_indexes[i]] == NSM; i++)
            {
                _types[i] = type;
            }
        }

        /// <summary>N1–N2: runs of neutrals and isolate controls between strong types.</summary>
        private void ResolveNeutralTypes()
        {
            var embedding = TypeForLevel(_level);
            for (int i = 0; i < _types.Length; i++)
            {
                if (!IsNeutralOrIsolate(_types[i])) continue;
                int limit = i + 1;
                while (limit < _types.Length && IsNeutralOrIsolate(_types[limit])) limit++;

                var leading = i == 0 ? _sos : StrongTypeForN1(_types[i - 1]);
                var trailing = limit == _types.Length ? _eos : StrongTypeForN1(_types[limit]);
                var resolved = leading == trailing ? leading : embedding;
                _types.AsSpan(i, limit - i).Fill(resolved);
                i = limit;
            }
        }

        private static bool IsNeutralOrIsolate(BidiClass type)
            => type is WS or ON or B or S or LRI or RLI or FSI or PDI;

        // Numbers act as R for the neutral rules; after the W rules only L, R, EN and AN remain.
        private static BidiClass StrongTypeForN1(BidiClass type) => type is EN or AN ? R : type;

        /// <summary>I1–I2, written straight to the paragraph's levels.</summary>
        private void ResolveImplicitLevels()
        {
            var levels = _paragraph._levels;
            bool even = (_level & 1) == 0;
            for (int i = 0; i < _indexes.Length; i++)
            {
                var t = _types[i];
                int level = _level;
                if (even)
                {
                    if (t == R) level += 1;
                    else if (t is AN or EN) level += 2;
                }
                else if (t is L or EN or AN)
                {
                    level += 1;
                }
                levels[_indexes[i]] = (byte)level;
            }
        }
    }
}
