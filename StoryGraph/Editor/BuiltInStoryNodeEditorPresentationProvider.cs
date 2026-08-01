using System.Collections.Generic;
using System.Linq;

namespace EightAID.StoryGraph.Editor
{
    internal sealed class BuiltInStoryNodeEditorPresentationProvider : IStoryNodeEditorPresentationProvider
    {
        public int Priority => 0;

        public IEnumerable<StoryNodeEditorPresentation> GetPresentations()
        {
            yield return new StoryNodeEditorPresentation(StoryNodeTypeIds.Root);
            yield return new StoryNodeEditorPresentation(
                StoryNodeTypeIds.Message,
                payload => BuildMessageSummary(payload as MessagePayload),
                payload => GetMessageSearchTexts(payload as MessagePayload),
                context =>
                {
                    context.AddProperty("_speakerName", "話者名");
                    context.AddProperty("_lines", "本文");
                });
            yield return new StoryNodeEditorPresentation(
                StoryNodeTypeIds.Branch,
                payload => BuildBranchSummary(payload as BranchPayload),
                payload => GetBranchSearchTexts(payload as BranchPayload),
                context =>
                {
                    context.AddProperty("_conditionKey", "条件キー");
                    context.AddProperty("_expectedValue", "期待値");
                });
            yield return new StoryNodeEditorPresentation(
                StoryNodeTypeIds.Delay,
                payload => payload is DelayPayload delay ? $"{delay.Seconds:0.###} 秒" : string.Empty,
                payload => payload is DelayPayload delay ? new[] { delay.Seconds.ToString("0.###") } : System.Array.Empty<string>(),
                context => context.AddProperty("_seconds", "待機秒数"));
            yield return new StoryNodeEditorPresentation(
                StoryNodeTypeIds.Comment,
                payload => payload is CommentPayload comment ? comment.Text : string.Empty,
                payload => payload is CommentPayload comment ? new[] { comment.Text } : System.Array.Empty<string>(),
                context => context.AddProperty("_text", "コメント"));
            yield return new StoryNodeEditorPresentation(StoryNodeTypeIds.End);
        }

        private static string BuildMessageSummary(MessagePayload payload)
        {
            if (payload == null)
            {
                return string.Empty;
            }

            string text = string.Join(" / ", payload.Lines.Where(line => !string.IsNullOrWhiteSpace(line)));
            return string.IsNullOrWhiteSpace(payload.SpeakerName)
                ? text
                : $"{payload.SpeakerName}: {text}";
        }

        private static IEnumerable<string> GetMessageSearchTexts(MessagePayload payload)
        {
            if (payload == null)
            {
                return System.Array.Empty<string>();
            }

            return new[] { payload.SpeakerName }.Concat(payload.Lines);
        }

        private static string BuildBranchSummary(BranchPayload payload)
        {
            return payload == null
                ? string.Empty
                : $"{payload.ConditionKey} = {payload.ExpectedValue}";
        }

        private static IEnumerable<string> GetBranchSearchTexts(BranchPayload payload)
        {
            return payload == null
                ? System.Array.Empty<string>()
                : new[] { payload.ConditionKey, payload.ExpectedValue };
        }
    }
}
