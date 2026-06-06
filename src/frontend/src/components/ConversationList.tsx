import type { ConversationDTO } from '../api/messages';
import ConversationSummaryCard from './ConversationSummaryCard';

interface ConversationListProps {
  conversations: ConversationDTO[];
  loading: boolean;
  error: string;
}

export default function ConversationList({ conversations, loading, error }: ConversationListProps) {
  return (
    <>
      {loading && <p className="text-sm text-[#5d7056]">Loading conversations...</p>}

      {error && (
        <p className="rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{error}</p>
      )}

      {!loading && !error && (
        <div className="flex flex-col gap-3">
          {conversations.length === 0 && (
            <p className="text-sm text-[#5d7056]">No conversations yet.</p>
          )}
          {conversations.map((conv) => (
            <ConversationSummaryCard
              key={conv.ConversationId}
              conversation={conv}
              isGroup={conv.Type === 'Group'}
            />
          ))}
        </div>
      )}
    </>
  );
}
