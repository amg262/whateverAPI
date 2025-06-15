import React, { useState } from 'react';
import { apiService } from '../services/api';
import { X, Loader2, Tag as TagIcon } from 'lucide-react';

type CreateJokeModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onJokeCreated: () => void;
};

export const CreateJokeModal: React.FC<CreateJokeModalProps> = ({
  isOpen,
  onClose,
  onJokeCreated,
}) => {
  const [content, setContent] = useState('');
  const [type, setType] = useState<number | ''>('');
  const [laughScore, setLaughScore] = useState<number | ''>('');
  const [tagInput, setTagInput] = useState('');
  const [tags, setTags] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleAddTag = () => {
    const trimmedTag = tagInput.trim();
    if (trimmedTag && !tags.includes(trimmedTag)) {
      setTags([...tags, trimmedTag]);
      setTagInput('');
    }
  };

  const handleRemoveTag = (tagToRemove: string) => {
    setTags(tags.filter(tag => tag !== tagToRemove));
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      handleAddTag();
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!content.trim()) {
      setError('Joke content is required');
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      const jokeData = {
        content: content.trim(),
        type: type === '' ? undefined : Number(type),
        tags: tags,
        isActive: true,
        laughScore: laughScore === '' ? null : Number(laughScore)
      };

      await apiService.createJoke(jokeData);
      
      // Reset form
      setContent('');
      setType('');
      setLaughScore('');
      setTags([]);
      setTagInput('');
      
      // Notify parent and close
      onJokeCreated();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create joke');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setContent('');
      setType('');
      setLaughScore('');
      setTags([]);
      setTagInput('');
      setError(null);
      onClose();
    }
  };

  if (!isOpen) return null;

  return (
    <>
      <style>
        {`
          .slider::-webkit-slider-thumb {
            appearance: none;
            height: 20px;
            width: 20px;
            border-radius: 50%;
            background: linear-gradient(135deg, #3b82f6, #8b5cf6);
            cursor: pointer;
            box-shadow: 0 2px 4px rgba(0,0,0,0.2);
            border: 2px solid white;
          }
          
          .slider::-moz-range-thumb {
            height: 20px;
            width: 20px;
            border-radius: 50%;
            background: linear-gradient(135deg, #3b82f6, #8b5cf6);
            cursor: pointer;
            box-shadow: 0 2px 4px rgba(0,0,0,0.2);
            border: 2px solid white;
          }
          
          .slider::-webkit-slider-track {
            height: 8px;
            border-radius: 4px;
            background: linear-gradient(to right, #ef4444, #f59e0b, #10b981, #8b5cf6);
          }
          
          .slider::-moz-range-track {
            height: 8px;
            border-radius: 4px;
            background: linear-gradient(to right, #ef4444, #f59e0b, #10b981, #8b5cf6);
          }
        `}
      </style>
      <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div className="bg-white/90 backdrop-blur-sm rounded-2xl p-6 shadow-2xl border border-white/20 w-full max-w-lg max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold text-gray-900">Create New Joke</h2>
          <button
            onClick={handleClose}
            disabled={isSubmitting}
            className="p-2 hover:bg-gray-100 rounded-lg transition-colors disabled:opacity-50"
            title="Close"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Content */}
          <div>
            <label htmlFor="content" className="block text-sm font-medium text-gray-700 mb-2">
              Joke Content *
            </label>
            <textarea
              id="content"
              value={content}
              onChange={(e) => setContent(e.target.value)}
              className="w-full p-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all bg-white/50 backdrop-blur-sm resize-none"
              rows={4}
              placeholder="Enter your joke here..."
              required
              disabled={isSubmitting}
            />
          </div>

          {/* Type and Laugh Score Side by Side */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Type */}
            <div>
              <label htmlFor="type" className="block text-sm font-medium text-gray-700 mb-2">
                Joke Type (Optional)
              </label>
              <select
                id="type"
                value={type}
                onChange={(e) => setType(e.target.value === '' ? '' : Number(e.target.value))}
                className="w-full p-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all bg-white/50 backdrop-blur-sm"
                disabled={isSubmitting}
              >
                <option value="">Select a type...</option>
                <option value="1">Type 1</option>
                <option value="2">Type 2</option>
                <option value="3">Type 3</option>
                <option value="4">Type 4</option>
                <option value="5">Type 5</option>
                <option value="6">Type 6</option>
                <option value="7">Type 7</option>
                <option value="8">Type 8</option>
                <option value="9">Type 9</option>
                <option value="10">Type 10</option>
              </select>
            </div>

            {/* Laugh Score Slider */}
            <div>
              <label htmlFor="laughScore" className="block text-sm font-medium text-gray-700 mb-2">
                Laugh Score: {laughScore || 0}
              </label>
              <div className="relative">
                <input
                  id="laughScore"
                  type="range"
                  min="0"
                  max="100"
                  value={laughScore || 0}
                  onChange={(e) => setLaughScore(Number(e.target.value))}
                  className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer slider"
                  disabled={isSubmitting}
                />
                <div className="flex justify-between text-xs text-gray-500 mt-1">
                  <span>0</span>
                  <span>50</span>
                  <span>100</span>
                </div>
              </div>
              <div className="mt-2 text-center">
                <div className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium ${
                  !laughScore || laughScore === 0 
                    ? 'bg-gray-100 text-gray-500' 
                    : laughScore <= 30 
                    ? 'bg-red-100 text-red-700' 
                    : laughScore <= 60 
                    ? 'bg-yellow-100 text-yellow-700' 
                    : laughScore <= 85 
                    ? 'bg-green-100 text-green-700' 
                    : 'bg-purple-100 text-purple-700'
                }`}>
                  {!laughScore || laughScore === 0 
                    ? 'No Score' 
                    : laughScore <= 30 
                    ? 'Mild 😐' 
                    : laughScore <= 60 
                    ? 'Funny 😊' 
                    : laughScore <= 85 
                    ? 'Hilarious 😂' 
                    : 'Legendary 🤣'
                  }
                </div>
              </div>
            </div>
          </div>

          {/* Tags */}
          <div>
            <label htmlFor="tags" className="block text-sm font-medium text-gray-700 mb-2">
              Tags (Optional)
            </label>
            <div className="flex gap-2 mb-2">
              <input
                id="tags"
                type="text"
                value={tagInput}
                onChange={(e) => setTagInput(e.target.value)}
                onKeyPress={handleKeyPress}
                className="flex-1 p-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all bg-white/50 backdrop-blur-sm"
                placeholder="Enter a tag and press Enter"
                disabled={isSubmitting}
              />
              <button
                type="button"
                onClick={handleAddTag}
                disabled={!tagInput.trim() || isSubmitting}
                className="bg-blue-100 hover:bg-blue-200 text-blue-700 px-4 py-3 rounded-xl font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Add
              </button>
            </div>
            
            {/* Display tags */}
            {tags.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {tags.map((tag, index) => (
                  <span
                    key={index}
                    className="bg-gray-100 text-gray-700 px-3 py-1 rounded-full text-sm flex items-center space-x-2"
                  >
                    <TagIcon className="w-3 h-3" />
                    <span>{tag}</span>
                                         <button
                       type="button"
                       onClick={() => handleRemoveTag(tag)}
                       disabled={isSubmitting}
                       className="text-gray-500 hover:text-red-500 transition-colors disabled:opacity-50"
                       title="Remove tag"
                     >
                       <X className="w-3 h-3" />
                     </button>
                  </span>
                ))}
              </div>
            )}
          </div>

          {/* Error Message */}
          {error && (
            <div className="bg-red-50 border border-red-200 rounded-xl p-4">
              <p className="text-red-600 text-sm">{error}</p>
            </div>
          )}

          {/* Submit Button */}
          <div className="flex gap-3 pt-4">
            <button
              type="button"
              onClick={handleClose}
              disabled={isSubmitting}
              className="flex-1 bg-gray-100 hover:bg-gray-200 text-gray-700 py-3 px-4 rounded-xl font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting || !content.trim()}
              className="flex-1 bg-gradient-to-r from-blue-600 to-purple-600 text-white py-3 px-4 rounded-xl font-medium hover:from-blue-700 hover:to-purple-700 focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center space-x-2"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="w-5 h-5 animate-spin" />
                  <span>Creating...</span>
                </>
              ) : (
                <span>Create Joke</span>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
    </>
  );
}; 