export default function PullUpLogo() {
  return (
    <svg viewBox="0 0 220 100" className="h-12 w-36" aria-label="PullUp logo" role="img">
      <g transform="translate(10, 15)">
        <path d="M 15 70 L 65 70 L 50 10 L 30 10 Z" fill="#f7fbf0" />
        <line x1="40" y1="65" x2="40" y2="15" stroke="#12351f" strokeWidth="4" strokeDasharray="10, 6" />
      </g>
      <text
        x="85"
        y="65"
        fontFamily="Segoe UI, Roboto, Helvetica, Arial, sans-serif"
        fontSize="38"
        fontWeight="900"
        fill="#f7fbf0"
        letterSpacing="-1"
      >
        Pull<tspan fill="#8cc63f">Up</tspan>
      </text>
    </svg>
  );
}
