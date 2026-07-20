"use client";

import { FormEvent, useState } from "react";
import { ApiError, jokesApi, JokeType } from "@/app/lib/api";
import { Button } from "@/app/components/ui/Button";

const JOKE_TYPES: JokeType[] = ["Joke", "FunnySaying", "Discouragement", "SelfDeprecating"];

interface JokeFormProps {
  onCreated?: () => void;
}

export function JokeForm({ onCreated }: JokeFormProps) {
  const [content, setContent] = useState("");
  const [type, setType] = useState<JokeType>("Joke");
  const [tags, setTags] = useState("");
  const [status, setStatus] = useState<"idle" | "submitting" | "error">("idle");
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setStatus("submitting");
    setError(null);

    try {
      await jokesApi.create({
        content,
        type,
        tags: tags
          .split(",")
          .map((t) => t.trim())
          .filter(Boolean),
        isActive: true,
      });
      setContent("");
      setTags("");
      setStatus("idle");
      onCreated?.();
    } catch (err) {
      setStatus("error");
      if (err instanceof ApiError && err.status === 401) {
        setError("You need to be signed in to submit a joke.");
      } else {
        setError(err instanceof Error ? err.message : "Failed to submit joke");
      }
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex w-full flex-col gap-3">
      <textarea
        value={content}
        onChange={(e) => setContent(e.target.value)}
        placeholder="Write something funny (or bleak)..."
        required
        minLength={5}
        rows={3}
        className="w-full rounded-lg border border-black/[.08] bg-transparent p-3 text-sm dark:border-white/[.145]"
      />

      <div className="flex flex-col gap-3 sm:flex-row">
        <select
          value={type}
          onChange={(e) => setType(e.target.value as JokeType)}
          className="rounded-lg border border-black/[.08] bg-transparent p-2 text-sm dark:border-white/[.145]"
        >
          {JOKE_TYPES.map((t) => (
            <option key={t} value={t}>
              {t}
            </option>
          ))}
        </select>

        <input
          value={tags}
          onChange={(e) => setTags(e.target.value)}
          placeholder="tags, comma, separated"
          className="flex-1 rounded-lg border border-black/[.08] bg-transparent p-2 text-sm dark:border-white/[.145]"
        />
      </div>

      {error && <p className="text-sm text-red-500">{error}</p>}

      <Button type="submit" disabled={status === "submitting"}>
        {status === "submitting" ? "Submitting..." : "Submit joke"}
      </Button>
    </form>
  );
}
