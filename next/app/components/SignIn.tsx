"use client";

import { FormEvent, useState, useSyncExternalStore } from "react";
import { auth } from "@/app/lib/api";
import { Button } from "@/app/components/ui/Button";

export function SignIn() {
  const signedInAs = useSyncExternalStore(auth.subscribe, auth.getEmail, auth.getServerEmail);

  const [email, setEmail] = useState("");
  const [name, setName] = useState("");
  const [status, setStatus] = useState<"idle" | "submitting">("idle");
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setStatus("submitting");
    setError(null);

    try {
      await auth.signIn(email, name);
      setEmail("");
      setName("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Sign in failed");
    } finally {
      setStatus("idle");
    }
  }

  if (signedInAs) {
    return (
      <div className="flex flex-wrap items-center gap-3 text-sm text-zinc-600 dark:text-zinc-400">
        <span>
          Signed in as <span className="font-medium">{signedInAs}</span>
        </span>
        <Button variant="secondary" onClick={() => auth.signOut()}>
          Sign out
        </Button>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3 sm:flex-row sm:items-start">
      <input
        value={name}
        onChange={(e) => setName(e.target.value)}
        placeholder="Name"
        required
        minLength={2}
        className="rounded-lg border border-black/[.08] bg-transparent p-2 text-sm dark:border-white/[.145]"
      />
      <input
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        type="email"
        placeholder="you@example.com"
        required
        className="flex-1 rounded-lg border border-black/[.08] bg-transparent p-2 text-sm dark:border-white/[.145]"
      />
      <Button type="submit" disabled={status === "submitting"}>
        {status === "submitting" ? "Signing in..." : "Sign in"}
      </Button>
      {error && <p className="text-sm text-red-500">{error}</p>}
    </form>
  );
}
