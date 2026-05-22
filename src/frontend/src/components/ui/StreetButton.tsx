interface StreetButtonProps {
  href: string;
  children: React.ReactNode;
}

export default function StreetButton({ href, children }: StreetButtonProps) {
  return (
    <a
      href={href}
      className="relative block overflow-hidden rounded-2xl bg-[#252a30] px-8 py-5 text-center text-lg font-black text-white shadow-xl shadow-black/20 transition hover:-translate-y-1 hover:bg-[#334155]"
    >
      <span
        className="absolute left-6 right-6 top-1/2 flex -translate-y-1/2 justify-between opacity-35"
        aria-hidden="true"
      >
        {Array.from({ length: 6 }).map((_, i) => (
          <span key={i} className="h-1 w-8 rounded-full bg-[#8cc63f]" />
        ))}
      </span>
      <span className="relative">{children}</span>
    </a>
  );
}
