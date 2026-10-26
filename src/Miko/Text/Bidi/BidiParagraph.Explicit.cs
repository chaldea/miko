using static Miko.Text.Bidi.BidiClass;

namespace Miko.Text.Bidi;

internal sealed partial class BidiParagraph
{
    /// <summary>BD9: pair each isolate initiator with the PDI that closes it.</summary>
    private void DetermineMatchingIsolates()
    {
        for (int i = 0; i < _length; i++)
        {
            _matchingIsolateInitiator[i] = -1;
        }

        for (int i = 0; i < _length; i++)
        {
            _matchingPdi[i] = -1;
            if (_initialTypes[i] is not (LRI or RLI or FSI)) continue;

            int depth = 1;
            _matchingPdi[i] = _length;
            for (int j = i + 1; j < _length; j++)
            {
                var type = _initialTypes[j];
                if (type is LRI or RLI or FSI)
                {
                    depth++;
                }
                else if (type == PDI && --depth == 0)
                {
                    _matchingPdi[i] = j;
                    _matchingIsolateInitiator[j] = i;
                    break;
                }
                else if (type == B)
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// P2–P3 over [start, end): the first strong character, skipping isolates, decides the level.
    /// Also decides the direction of an FSI from the text up to its matching PDI.
    /// </summary>
    private byte DetermineParagraphLevel(int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            var type = _initialTypes[i];
            if (type == L) return 0;
            if (type is R or AL) return 1;
            if (type is LRI or RLI or FSI)
            {
                i = _matchingPdi[i];
            }
            else if (type == B)
            {
                break;
            }
        }
        return 0;
    }

    /// <summary>X1–X8: explicit embedding levels and directional overrides.</summary>
    private void DetermineExplicitLevels()
    {
        // The directional status stack (X1). Override is L, R or ON (none).
        var stackLevel = new byte[MaxDepth + 2];
        var stackOverride = new BidiClass[MaxDepth + 2];
        var stackIsolate = new bool[MaxDepth + 2];
        int depth = 0;
        stackLevel[0] = _paragraphLevel;
        stackOverride[0] = ON;
        stackIsolate[0] = false;

        int overflowIsolates = 0;
        int overflowEmbeddings = 0;
        int validIsolates = 0;

        for (int i = 0; i < _length; i++)
        {
            var type = _resultTypes[i];
            switch (type)
            {
                case RLE or LRE or RLO or LRO:
                {
                    // X2–X5.
                    bool rtl = type is RLE or RLO;
                    int current = stackLevel[depth];
                    int next = rtl ? (current + 1) | 1 : (current + 2) & ~1;
                    if (next <= MaxDepth && overflowIsolates == 0 && overflowEmbeddings == 0)
                    {
                        depth++;
                        stackLevel[depth] = (byte)next;
                        stackOverride[depth] = type == RLO ? R : type == LRO ? L : ON;
                        stackIsolate[depth] = false;
                    }
                    else if (overflowIsolates == 0)
                    {
                        overflowEmbeddings++;
                    }
                    // Removed by X9; level assigned afterwards.
                    _levels[i] = stackLevel[depth];
                    break;
                }

                case RLI or LRI or FSI:
                {
                    // X5a–X5c. The initiator itself takes the level outside the isolate.
                    _levels[i] = stackLevel[depth];
                    if (stackOverride[depth] != ON) _resultTypes[i] = stackOverride[depth];

                    bool rtl = type == RLI
                        || (type == FSI && DetermineParagraphLevel(i + 1, _matchingPdi[i]) == 1);
                    int current = stackLevel[depth];
                    int next = rtl ? (current + 1) | 1 : (current + 2) & ~1;
                    if (next <= MaxDepth && overflowIsolates == 0 && overflowEmbeddings == 0)
                    {
                        validIsolates++;
                        depth++;
                        stackLevel[depth] = (byte)next;
                        stackOverride[depth] = ON;
                        stackIsolate[depth] = true;
                    }
                    else
                    {
                        overflowIsolates++;
                    }
                    break;
                }

                case PDI:
                {
                    // X6a.
                    if (overflowIsolates > 0)
                    {
                        overflowIsolates--;
                    }
                    else if (validIsolates > 0)
                    {
                        overflowEmbeddings = 0;
                        while (!stackIsolate[depth]) depth--;
                        depth--;
                        validIsolates--;
                    }
                    _levels[i] = stackLevel[depth];
                    if (stackOverride[depth] != ON) _resultTypes[i] = stackOverride[depth];
                    break;
                }

                case PDF:
                {
                    // X7.
                    if (overflowIsolates == 0)
                    {
                        if (overflowEmbeddings > 0)
                        {
                            overflowEmbeddings--;
                        }
                        else if (!stackIsolate[depth] && depth > 0)
                        {
                            depth--;
                        }
                    }
                    _levels[i] = stackLevel[depth];
                    break;
                }

                case B:
                {
                    // X8: a paragraph separator ends every embedding. Text after it (only possible
                    // when a caller passes several paragraphs at once) restarts at the paragraph level.
                    depth = 0;
                    overflowIsolates = 0;
                    overflowEmbeddings = 0;
                    validIsolates = 0;
                    _levels[i] = _paragraphLevel;
                    break;
                }

                default:
                {
                    // X6. BN is removed by X9 and keeps its type.
                    _levels[i] = stackLevel[depth];
                    if (type != BN && stackOverride[depth] != ON) _resultTypes[i] = stackOverride[depth];
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Characters removed by X9 take the level of the preceding character (or the paragraph level
    /// at the start), so they never split a run they sit inside of.
    /// </summary>
    private void AssignLevelsToCharactersRemovedByX9()
    {
        for (int i = 0; i < _length; i++)
        {
            if (!IsRemovedByX9(_initialTypes[i])) continue;
            _levels[i] = i == 0 ? _paragraphLevel : _levels[i - 1];
        }
    }

    /// <summary>BD13 / X10: chain level runs into isolating run sequences.</summary>
    private List<IsolatingRunSequence> DetermineIsolatingRunSequences()
    {
        var runs = DetermineLevelRuns();
        var runForCharacter = new int[_length];
        for (int r = 0; r < runs.Count; r++)
        {
            foreach (int index in runs[r])
            {
                runForCharacter[index] = r;
            }
        }

        var sequences = new List<IsolatingRunSequence>();
        var current = new List<int>();
        foreach (var run in runs)
        {
            int first = run[0];
            // A run starting with the PDI of a matched initiator continues that initiator's sequence.
            if (_initialTypes[first] == PDI && _matchingIsolateInitiator[first] != -1) continue;

            current.Clear();
            var nextRun = run;
            while (true)
            {
                current.AddRange(nextRun);
                int last = current[^1];
                if (_initialTypes[last] is LRI or RLI or FSI && _matchingPdi[last] != _length)
                {
                    nextRun = runs[runForCharacter[_matchingPdi[last]]];
                    continue;
                }
                break;
            }
            sequences.Add(new IsolatingRunSequence(this, current.ToArray()));
        }
        return sequences;
    }

    /// <summary>BD7: maximal runs of equal level, ignoring characters removed by X9.</summary>
    private List<int[]> DetermineLevelRuns()
    {
        var runs = new List<int[]>();
        var run = new List<int>();
        int runLevel = -1;
        for (int i = 0; i < _length; i++)
        {
            if (IsRemovedByX9(_initialTypes[i])) continue;
            if (_levels[i] != runLevel && run.Count > 0)
            {
                runs.Add(run.ToArray());
                run.Clear();
            }
            run.Add(i);
            runLevel = _levels[i];
        }
        if (run.Count > 0) runs.Add(run.ToArray());
        return runs;
    }
}
