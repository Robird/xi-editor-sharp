using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Xi.Core.Rope;

internal static class EngineJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static string Serialize(Engine engine)
    {
        if (engine == null)
        {
            throw new ArgumentNullException(nameof(engine));
        }

        var dto = new EngineDto
        {
            Text = engine.TextSnapshot(),
            Tombstones = engine.TombstonesSnapshot(),
            DeletesFromUnion = ToSubsetDto(engine.DeletesFromUnionSnapshot()),
            UndoneGroups = CopyGroups(engine.UndoneGroupsSnapshot()),
            Revisions = ToRevisionDtos(engine.RevisionLog())
        };

        return JsonSerializer.Serialize(dto, SerializerOptions);
    }

    internal static Engine Deserialize(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        var dto = JsonSerializer.Deserialize<EngineDto>(json, SerializerOptions)
                  ?? throw new InvalidOperationException("Engine JSON payload could not be deserialized.");

        if (dto.Text == null)
        {
            throw new InvalidOperationException("Engine JSON payload must include text.");
        }

        if (dto.Tombstones == null)
        {
            throw new InvalidOperationException("Engine JSON payload must include tombstones.");
        }

        if (dto.DeletesFromUnion == null)
        {
            throw new InvalidOperationException("Engine JSON payload must include deletes_from_union.");
        }

        if (dto.UndoneGroups == null)
        {
            throw new InvalidOperationException("Engine JSON payload must include undone_groups.");
        }

        if (dto.Revisions == null)
        {
            throw new InvalidOperationException("Engine JSON payload must include revs.");
        }

        var deletesFromUnion = FromSubsetDto(dto.DeletesFromUnion);
        var undoneGroups = ValidateGroups(dto.UndoneGroups, "undone_groups");

        var revisions = new List<Revision>(dto.Revisions.Length);
        foreach (var revisionDto in dto.Revisions)
        {
            if (revisionDto == null)
            {
                throw new InvalidOperationException("Revision entry must not be null.");
            }

            if (revisionDto.RevId == null)
            {
                throw new InvalidOperationException("Revision must include rev_id.");
            }

            var revIdDto = revisionDto.RevId;
            var revId = new RevId(revIdDto.Session1, revIdDto.Session2, revIdDto.Number);

            if (revisionDto.MaxUndoSoFar < 0)
            {
                throw new InvalidOperationException("max_undo_so_far must be non-negative.");
            }

            if (revisionDto.Edit == null)
            {
                throw new InvalidOperationException("Revision must include an edit payload.");
            }

            var envelope = revisionDto.Edit;
            var hasEdit = envelope.Edit != null;
            var hasUndo = envelope.Undo != null;

            if (hasEdit == hasUndo)
            {
                throw new InvalidOperationException("Revision edit payload must specify exactly one of Edit or Undo.");
            }

            RevisionOperation operation;
            if (hasEdit)
            {
                var editDto = envelope.Edit!;

                if (editDto.Priority < 0)
                {
                    throw new InvalidOperationException("Edit priority must be non-negative.");
                }

                if (editDto.UndoGroup < 0)
                {
                    throw new InvalidOperationException("Edit undo_group must be non-negative.");
                }

                if (editDto.Inserts == null)
                {
                    throw new InvalidOperationException("Edit payload must include inserts.");
                }

                if (editDto.Deletes == null)
                {
                    throw new InvalidOperationException("Edit payload must include deletes.");
                }

                var inserts = FromSubsetDto(editDto.Inserts);
                var deletes = FromSubsetDto(editDto.Deletes);
                operation = new RevisionEdit(editDto.Priority, editDto.UndoGroup, inserts, deletes);
            }
            else
            {
                var undoDto = envelope.Undo!;

                if (undoDto.ToggledGroups == null)
                {
                    throw new InvalidOperationException("Undo payload must include toggled_groups.");
                }

                if (undoDto.DeletesBitxor == null)
                {
                    throw new InvalidOperationException("Undo payload must include deletes_bitxor.");
                }

                var toggledGroups = ValidateGroups(undoDto.ToggledGroups, "toggled_groups");
                var deletesBitxor = FromSubsetDto(undoDto.DeletesBitxor);
                operation = new RevisionUndo(toggledGroups, deletesBitxor);
            }

            revisions.Add(new Revision(revId, revisionDto.MaxUndoSoFar, operation));
        }

        return Engine.FromSerializedState(dto.Text, dto.Tombstones, deletesFromUnion, undoneGroups, revisions);
    }

    private static int[] CopyGroups(IReadOnlyList<int> groups)
    {
        if (groups.Count == 0)
        {
            return Array.Empty<int>();
        }

        var copy = new int[groups.Count];
        for (var i = 0; i < groups.Count; i++)
        {
            copy[i] = groups[i];
        }

        return copy;
    }

    private static int[] ValidateGroups(int[] groups, string fieldName)
    {
        if (groups == null)
        {
            throw new InvalidOperationException($"{fieldName} must not be null.");
        }

        if (groups.Length == 0)
        {
            return Array.Empty<int>();
        }

        var copy = new int[groups.Length];
        for (var i = 0; i < groups.Length; i++)
        {
            var value = groups[i];
            if (value < 0)
            {
                throw new InvalidOperationException($"{fieldName} values must be non-negative.");
            }

            copy[i] = value;
        }

        return copy;
    }

    private static RevisionDto[] ToRevisionDtos(IReadOnlyList<Revision> revisions)
    {
        if (revisions.Count == 0)
        {
            return Array.Empty<RevisionDto>();
        }

        var array = new RevisionDto[revisions.Count];
        for (var i = 0; i < revisions.Count; i++)
        {
            array[i] = ToRevisionDto(revisions[i]);
        }

        return array;
    }

    private static RevisionDto ToRevisionDto(Revision revision)
    {
        var envelope = new RevisionEditEnvelopeDto();
        if (revision.Operation.IsEdit)
        {
            envelope.Edit = ToEditPayloadDto(revision.Operation.AsEdit());
        }
        else
        {
            envelope.Undo = ToUndoPayloadDto(revision.Operation.AsUndo());
        }

        return new RevisionDto
        {
            RevId = new RevIdDto
            {
                Session1 = revision.RevId.Session1,
                Session2 = revision.RevId.Session2,
                Number = revision.RevId.Number
            },
            MaxUndoSoFar = revision.MaxUndoSoFar,
            Edit = envelope
        };
    }

    private static EditPayloadDto ToEditPayloadDto(RevisionEdit edit)
    {
        return new EditPayloadDto
        {
            Priority = edit.Priority,
            UndoGroup = edit.UndoGroup,
            Inserts = ToSubsetDto(edit.Inserts),
            Deletes = ToSubsetDto(edit.Deletes)
        };
    }

    private static UndoPayloadDto ToUndoPayloadDto(RevisionUndo undo)
    {
        return new UndoPayloadDto
        {
            ToggledGroups = CopyGroups(undo.ToggledGroups),
            DeletesBitxor = ToSubsetDto(undo.DeletesBitxor)
        };
    }

    private static SubsetDto ToSubsetDto(Subset subset)
    {
        var segments = new SegmentDto[subset.SegmentCount];
        var index = 0;
        foreach (var triple in subset.SegmentTriples())
        {
            segments[index++] = new SegmentDto
            {
                Length = triple.Length,
                Count = triple.Count
            };
        }

        return new SubsetDto
        {
            Segments = segments
        };
    }

    private static Subset FromSubsetDto(SubsetDto dto)
    {
        if (dto.Segments == null)
        {
            throw new InvalidOperationException("Subset segments array must be present.");
        }

        if (dto.Segments.Length == 0)
        {
            return Subset.Empty;
        }

        var triples = new List<(int Start, int Length, int Count)>(dto.Segments.Length);
        var offset = 0;
        foreach (var segment in dto.Segments)
        {
            if (segment.Length <= 0)
            {
                throw new InvalidOperationException("Segment length must be positive.");
            }

            if (segment.Count < 0)
            {
                throw new InvalidOperationException("Segment count must be non-negative.");
            }

            triples.Add((offset, segment.Length, segment.Count));
            offset += segment.Length;
        }

        return Subset.FromSegmentTriples(triples);
    }

    private sealed class EngineDto
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("tombstones")]
        public string? Tombstones { get; set; }

        [JsonPropertyName("deletes_from_union")]
        public SubsetDto? DeletesFromUnion { get; set; }

        [JsonPropertyName("undone_groups")]
        public int[]? UndoneGroups { get; set; }

        [JsonPropertyName("revs")]
        public RevisionDto[]? Revisions { get; set; }
    }

    private sealed class RevisionDto
    {
        [JsonPropertyName("rev_id")]
        public RevIdDto? RevId { get; set; }

        [JsonPropertyName("max_undo_so_far")]
        public int MaxUndoSoFar { get; set; }

        [JsonPropertyName("edit")]
        public RevisionEditEnvelopeDto? Edit { get; set; }
    }

    private sealed class RevIdDto
    {
        [JsonPropertyName("session1")]
        public long Session1 { get; set; }

        [JsonPropertyName("session2")]
        public int Session2 { get; set; }

        [JsonPropertyName("num")]
        public int Number { get; set; }
    }

    private sealed class RevisionEditEnvelopeDto
    {
        [JsonPropertyName("Edit")]
        public EditPayloadDto? Edit { get; set; }

        [JsonPropertyName("Undo")]
        public UndoPayloadDto? Undo { get; set; }
    }

    private sealed class EditPayloadDto
    {
        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("undo_group")]
        public int UndoGroup { get; set; }

        [JsonPropertyName("inserts")]
        public SubsetDto? Inserts { get; set; }

        [JsonPropertyName("deletes")]
        public SubsetDto? Deletes { get; set; }
    }

    private sealed class UndoPayloadDto
    {
        [JsonPropertyName("toggled_groups")]
        public int[]? ToggledGroups { get; set; }

        [JsonPropertyName("deletes_bitxor")]
        public SubsetDto? DeletesBitxor { get; set; }
    }

    private sealed class SubsetDto
    {
        [JsonPropertyName("segments")]
        public SegmentDto[]? Segments { get; set; }
    }

    private sealed class SegmentDto
    {
        [JsonPropertyName("len")]
        public int Length { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
