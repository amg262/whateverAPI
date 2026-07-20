"use client";

import { useCallback, useEffect, useState } from "react";
import { ApiError, Joke, jokesApi } from "@/app/lib/api";
import { JokeForm } from "@/app/components/JokeForm";
import { Button } from "@/app/components/ui/Button";

export default function Home() {
  const [joke, setJoke] = useState<Joke | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadRandomJoke = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setJoke(await jokesApi.getRandom());
    } catch (err) {
      setJoke(null);
      if (err instanceof ApiError) {
        setError(err.status === 404 ? "No jokes yet — add one below." : err.message);
      } else {
        setError("Couldn't reach the API. Is it running?");
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    let ignore = false;

    jokesApi
      .getRandom()
      .then((j) => {
        if (!ignore) setJoke(j);
      })
      .catch((err) => {
        if (ignore) return;
        setJoke(null);
        setError(
          err instanceof ApiError && err.status === 404
            ? "No jokes yet — add one below."
            : "Couldn't reach the API. Is it running?",
        );
      })
      .finally(() => {
        if (!ignore) setLoading(false);
      });

    return () => {
      ignore = true;
    };
  }, []);

  return (
    <div className="flex flex-1 flex-col items-center bg-zinc-50 font-sans dark:bg-black">
      <main className="flex w-full max-w-2xl flex-1 flex-col gap-10 px-6 py-16 sm:px-10">
        <header className="flex flex-col gap-2">
          <h1 className="text-3xl font-semibold tracking-tight text-black dark:text-zinc-50">
            Whatever
          </h1>
          <p className="text-zinc-600 dark:text-zinc-400">
            A joke management system with a taste for dark humor.
          </p>
        </header>

        <section className="flex flex-col gap-4 rounded-xl border border-black/[.08] p-6 dark:border-white/[.145]">
          {loading && <p className="text-zinc-500">Loading a joke...</p>}

          {!loading && error && <p className="text-red-500">{error}</p>}

          {!loading && !error && joke && (
            <>
              <p className="text-lg text-black dark:text-zinc-50">{joke.content}</p>
              <div className="flex flex-wrap items-center gap-2 text-sm text-zinc-500">
                {joke.type && (
                  <span className="rounded-full bg-black/[.05] px-2.5 py-1 dark:bg-white/[.08]">
                    {joke.type}
                  </span>
                )}
                {joke.tags?.map((tag) => (
                  <span key={tag} className="rounded-full bg-black/[.05] px-2.5 py-1 dark:bg-white/[.08]">
                    #{tag}
                  </span>
                ))}
                {joke.laughScore != null && <span>Laugh score: {joke.laughScore}</span>}
              </div>
            </>
          )}

          <Button variant="secondary" onClick={loadRandomJoke} disabled={loading} className="self-start">
            {loading ? "Loading..." : "Get another joke"}
          </Button>
        </section>

        <section className="flex flex-col gap-4">
          <h2 className="text-xl font-semibold text-black dark:text-zinc-50">Submit a joke</h2>
          <JokeForm onCreated={loadRandomJoke} />
        </section>
      </main>
    </div>
  );
}
