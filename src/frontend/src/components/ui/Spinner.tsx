interface SpinnerProps {
  label?: string;
}

export default function Spinner({ label }: SpinnerProps) {
  return (
    <div className="flex flex-col items-center gap-3 py-8">
      <div className="h-10 w-10 animate-spin rounded-full border-4 border-[#d7e8c8] border-t-[#8cc63f]" />
      {label && <p className="text-sm text-[#5d7056]">{label}</p>}
    </div>
  );
}
