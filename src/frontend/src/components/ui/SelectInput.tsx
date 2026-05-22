interface SelectInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
}

const SEX_OPTIONS = [
  { value: 'male', label: 'Male' },
  { value: 'female', label: 'Female' },
  { value: 'other', label: 'Other' },
  { value: 'not-specified', label: 'Prefer not to say' },
];

export default function SelectInput({ label, value, onChange }: SelectInputProps) {
  return (
    <label className="block">
      <span className="text-sm text-[#5d7056]">{label}</span>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="mt-1 h-12 w-full rounded-xl border border-[#d7e8c8] bg-white px-4 font-semibold text-[#12351f] outline-none focus:border-[#8cc63f]"
      >
        <option value="">Choose</option>
        {SEX_OPTIONS.map((opt) => (
          <option key={opt.value} value={opt.value}>{opt.label}</option>
        ))}
      </select>
    </label>
  );
}
