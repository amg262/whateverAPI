import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Edit2, Trash2, Share2, Copy, Heart, MessageSquare } from 'lucide-react';
import type { Joke } from '../types';
import { getJokeTypeLabel } from '../utils/jokeTypeUtils';
import toast from 'react-hot-toast';

type JokeCardProps = {
  joke: Joke;
  onDelete?: (id: string) => void;
  deleteLoading?: boolean;
  showActions?: boolean;
  className?: string;
}

const JokeCard: React.FC<JokeCardProps> = ({ 
  joke, 
  onDelete, 
  deleteLoading = false,
  showActions = true,
  className = '' 
}) => {
  const [liked, setLiked] = useState(false);
  const [showShareMenu, setShowShareMenu] = useState(false);

  const handleShare = () => {
    setShowShareMenu(!showShareMenu);
  };

  const handleCopyToClipboard = () => {
    const shareText = `"${joke.content}" - ${getJokeTypeLabel(joke.type)} joke from JokeApp`;
    navigator.clipboard.writeText(shareText);
    toast.success('Joke copied to clipboard!');
    setShowShareMenu(false);
  };

  const handleShareToSocial = (platform: 'twitter' | 'facebook') => {
    const shareText = encodeURIComponent(`"${joke.content}" - Check out this ${getJokeTypeLabel(joke.type)} joke!`);
    const url = window.location.href;
    
    let shareUrl = '';
    if (platform === 'twitter') {
      shareUrl = `https://twitter.com/intent/tweet?text=${shareText}&url=${url}`;
    } else if (platform === 'facebook') {
      shareUrl = `https://www.facebook.com/sharer/sharer.php?u=${url}&quote=${shareText}`;
    }
    
    window.open(shareUrl, '_blank', 'width=550,height=420');
    setShowShareMenu(false);
  };

  const handleLike = () => {
    setLiked(!liked);
    toast.success(liked ? 'Like removed!' : 'Joke liked!');
  };

  const handleDelete = () => {
    if (onDelete && confirm('Are you sure you want to delete this joke?')) {
      onDelete(joke.id);
    }
  };

  return (
    <div className={`bg-white rounded-lg shadow-md p-6 joke-card relative ${className}`}>
      <div className="flex justify-between items-start mb-4">
        <div className="flex-1">
          <div className="flex items-center space-x-2 mb-2">
            <span className="bg-blue-100 text-blue-800 px-2 py-1 rounded text-sm">
              {getJokeTypeLabel(joke.type)}
            </span>
            {joke.laughScore && (
              <span className="bg-yellow-100 text-yellow-800 px-2 py-1 rounded text-sm">
                Score: {joke.laughScore}/10
              </span>
            )}
            {!joke.isActive && (
              <span className="bg-gray-100 text-gray-600 px-2 py-1 rounded text-sm">
                Inactive
              </span>
            )}
          </div>
          
          <p className="text-lg text-gray-800 mb-3 leading-relaxed">
            {joke.content}
          </p>
          
          {joke.tags.length > 0 && (
            <div className="flex flex-wrap gap-1 mb-3">
              {joke.tags.map((tag) => (
                <span
                  key={tag.id}
                  className="bg-gray-100 text-gray-700 px-2 py-1 rounded text-sm"
                >
                  #{tag.name}
                </span>
              ))}
            </div>
          )}
          
          <div className="text-sm text-gray-500">
            <p>By: {joke.user?.name || 'Unknown'}</p>
            <p>Created: {new Date(joke.createdAt).toLocaleDateString()}</p>
          </div>
        </div>
        
        {showActions && (
          <div className="flex space-x-2 ml-4">
            <Link
              to={`/edit-joke/${joke.id}`}
              className="p-2 text-gray-400 hover:text-blue-600 transition-colors"
              title="Edit joke"
            >
              <Edit2 className="h-4 w-4" />
            </Link>
            <button
              onClick={handleDelete}
              disabled={deleteLoading}
              className="p-2 text-gray-400 hover:text-red-600 transition-colors disabled:opacity-50"
              title="Delete joke"
            >
              <Trash2 className="h-4 w-4" />
            </button>
          </div>
        )}
      </div>

      {/* Interaction Bar */}
      <div className="flex items-center justify-between pt-3 border-t border-gray-100">
        <div className="flex space-x-4">
          <button
            onClick={handleLike}
            className={`flex items-center space-x-1 px-3 py-1 rounded transition-colors ${
              liked 
                ? 'text-red-600 bg-red-50' 
                : 'text-gray-500 hover:text-red-600 hover:bg-red-50'
            }`}
          >
            <Heart className={`h-4 w-4 ${liked ? 'fill-current' : ''}`} />
            <span className="text-sm">Like</span>
          </button>
          
          <button
            className="flex items-center space-x-1 px-3 py-1 rounded text-gray-500 hover:text-blue-600 hover:bg-blue-50 transition-colors"
            title="Comment (Coming Soon)"
          >
            <MessageSquare className="h-4 w-4" />
            <span className="text-sm">Comment</span>
          </button>
        </div>

        <div className="relative">
          <button
            onClick={handleShare}
            className="flex items-center space-x-1 px-3 py-1 rounded text-gray-500 hover:text-green-600 hover:bg-green-50 transition-colors"
          >
            <Share2 className="h-4 w-4" />
            <span className="text-sm">Share</span>
          </button>
          
          {showShareMenu && (
            <div className="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg ring-1 ring-black ring-opacity-5 z-10">
              <div className="py-1">
                <button
                  onClick={handleCopyToClipboard}
                  className="flex items-center space-x-2 w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                >
                  <Copy className="h-4 w-4" />
                  <span>Copy to clipboard</span>
                </button>
                <button
                  onClick={() => handleShareToSocial('twitter')}
                  className="flex items-center space-x-2 w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                >
                  <span>Share on Twitter</span>
                </button>
                <button
                  onClick={() => handleShareToSocial('facebook')}
                  className="flex items-center space-x-2 w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                >
                  <span>Share on Facebook</span>
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default JokeCard; 