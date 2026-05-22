interface TextInputProps {
  label: string;
  type?: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}

export default function TextInput({ label, type = 'text', value, onChange, placeholder }: TextInputProps) {
  return (
    <label className="block">
      <span className="text-sm text-[#5d7056]">{label}</span>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="mt-1 h-12 w-full rounded-xl border border-[#d7e8c8] bg-white px-4 font-semibold text-[#12351f] outline-none focus:border-[#8cc63f]"
      />
    </label>
  );
}
