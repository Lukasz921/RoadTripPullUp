interface NumberInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  min?: number;
  step?: number;
}

export default function NumberInput({ label, value, onChange, placeholder, min = 0, step = 1 }: NumberInputProps) {
  return (
    <label className="block">
      <span className="text-sm text-[#5d7056]">{label}</span>
      <input
        type="number"
        min={min}
        step={step}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        // Prevent mouse-wheel scrolling from silently changing the value.
        onWheel={(e) => e.currentTarget.blur()}
        placeholder={placeholder}
        className="mt-1 h-12 w-full rounded-xl border border-[#d7e8c8] bg-white px-4 font-semibold text-[#12351f] outline-none focus:border-[#8cc63f]"
      />
    </label>
  );
}
