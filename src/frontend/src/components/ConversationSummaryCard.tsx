import type { ConversationDTO } from '../api/messages';
import { formatDate } from '../utils/format';

interface ConversationSummaryCardProps {
  conversation: ConversationDTO;
  isGroup?: boolean;
}

export default function ConversationSummaryCard({ conversation, isGroup }: ConversationSummaryCardProps) {
  const title = conversation.Name ?? (isGroup ? 'Group chat' : 'Direct chat');

  return (
    <div
      className={`rounded-xl border bg-white px-5 py-4 ${
        isGroup ? 'border-[#8cc63f] border-l-4' : 'border-[#d7e8c8]'
      }`}
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-2">
          {isGroup && (
            <span className="rounded-full bg-[#8cc63f] px-2 py-0.5 text-xs font-semibold text-white">
              Group
            </span>
          )}
          <p className="font-semibold text-[#12351f]">{title}</p>
        </div>
        <p className="shrink-0 text-xs text-[#5d7056]">
          {conversation.LastMessageCreatedAt ? formatDate(conversation.LastMessageCreatedAt) : ''}
        </p>
      </div>

      {conversation.LastMessagePreview && (
        <p className="mt-1 truncate text-sm text-[#5d7056]">{conversation.LastMessagePreview}</p>
      )}

      <p className="mt-2 text-xs text-[#5d7056]">
        {conversation.Participants.length} participant{conversation.Participants.length !== 1 ? 's' : ''}
      </p>
    </div>
  );
}
