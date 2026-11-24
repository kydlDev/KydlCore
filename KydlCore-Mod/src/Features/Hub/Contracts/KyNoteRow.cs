using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>Explaining text (made by <see cref="KySection.Note"/>).</summary>
public sealed class KyNoteRow : KyRow
{
    internal KyNoteRow(string text) : base(text) { }
}
