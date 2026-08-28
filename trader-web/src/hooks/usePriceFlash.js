import { useEffect, useRef, useState } from "react";

/**
 * Tracks whether a price moved up or down since the last render. Returns a
 * `flashKey` that increments on every change — used as a React `key` on the
 * price cell so React remounts that one small element, cleanly restarting
 * the CSS flash animation without any manual timers.
 */
export function usePriceFlash(price) {
  const previousRef = useRef(price);
  const [direction, setDirection] = useState(null);
  const [flashKey, setFlashKey] = useState(0);

  useEffect(() => {
    const previous = previousRef.current;
    if (previous !== undefined && price !== previous) {
      setDirection(price > previous ? "up" : "down");
      setFlashKey((key) => key + 1);
    }
    previousRef.current = price;
  }, [price]);

  return { direction, flashKey };
}