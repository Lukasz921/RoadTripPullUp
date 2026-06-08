interface StreetButtonProps {
  href?: string;
  onClick?: () => void;
  children: React.ReactNode;
  className?: string;
}

const INNER_CLASS = 'relative block overflow-hidden rounded-2xl bg-[#252a30] px-8 py-5 text-center text-lg font-black text-white shadow-xl shadow-black/20 transition hover:-translate-y-1 hover:bg-[#334155]';

const Stripes = () => (
  <span
    className="absolute left-6 right-6 top-1/2 flex -translate-y-1/2 justify-between opacity-35"
    aria-hidden="true"
  >
    {Array.from({ length: 6 }).map((_, i) => (
      <span key={i} className="h-1 w-8 rounded-full bg-[#8cc63f]" />
    ))}
  </span>
);

export default function StreetButton({ href, onClick, children, className = '' }: StreetButtonProps) {
  if (onClick) {
    return (
      <button
        type="button"
        onClick={onClick}
        className={`${INNER_CLASS} w-full cursor-pointer ${className}`}
      >
        <Stripes />
        <span className="relative">{children}</span>
      </button>
    );
  }

  return (
    <a href={href} className={`${INNER_CLASS} ${className}`}>
      <Stripes />
      <span className="relative">{children}</span>
    </a>
  );
}
