interface ProfileRowProps {
  label: string;
  value: string | number;
}

export default function ProfileRow({ label, value }: ProfileRowProps) {
  return (
    <div>
      <p className="text-sm text-[#5d7056]">{label}</p>
      <p className="font-semibold text-[#12351f]">{value}</p>
    </div>
  );
}
