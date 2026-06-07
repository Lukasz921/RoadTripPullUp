using Application.Exceptions;

namespace MessageService.Application.Helpers;

public static class FromToIntoSkipTake
{
    public static void Convert(int? from, int? to, out int skip, out int take)
    {
        if (from != null && to != null)
        {
            if (from < 0 || to < 0)
                throw new InvalidParametersException("fromConversation and toConversation must be non-negative");
            if (to < from) throw new InvalidParametersException("toConversation must be >= fromConversation");

            skip = from.Value;
            // inclusive range: from..to => count = to - from + 1
            try
            {
                checked
                {
                    take = to.Value - from.Value + 1;
                }
            }
            catch (OverflowException)
            {
                throw new InvalidParametersException("range too large");
            }
        }
        else if (from != null)
        {
            if (from < 0) throw new InvalidParametersException("fromConversation must be non-negative");
            skip = from.Value;
            take = 20; // default window size
        }
        else if (to != null)
        {
            if (to < 0) throw new InvalidParametersException("toConversation must be non-negative");
            skip = 0;
            take = to.Value + 1; // take first (to+1) items
        }
        else
        {
            // no range provided: default to first page/window
            skip = 0;
            take = 20;
        }
    }
}