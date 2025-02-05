'use client';

import { useState } from 'react';

export default function JokeForm() {
    const [jokeContent, setJokeContent] = useState('');

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        // TODO: Add API call to create joke
        console.log('Submitting joke:', jokeContent);
    };

    return (
        <div className="min-h-screen flex items-center justify-center bg-gray-100">
            <div className="p-8 bg-white rounded-lg shadow-md w-full max-w-md">
                <h2 className="text-2xl font-bold mb-6 text-center text-gray-800">Add a Joke</h2>
                <form onSubmit={handleSubmit} className="space-y-4">
                    <div>

                        <div>{"Hello everyone!"}</div>
            <textarea
                value={jokeContent}
                onChange={(e) => setJokeContent(e.target.value)}
                className="w-full p-3 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                rows={4}
                placeholder="Enter your joke here..."
            />
                    </div>
                    <button
                        type="submit"
                        className="w-full bg-blue-500 text-white py-2 px-4 rounded-md hover:bg-blue-600 transition-colors"
                    >
                        Submit Joke
                    </button>
                </form>
            </div>
        </div>
    );
}