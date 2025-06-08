import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, X } from 'lucide-react';
import type { CreateJokeRequest, Tag, JokeType } from '../types';
import { JokeType as JokeTypeEnum } from '../types';
import PageLayout from '../components/PageLayout';
import apiService from '../services/api';
import toast from 'react-hot-toast';

const CreateJokePage: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [availableTags, setAvailableTags] = useState<Tag[]>([]);
  const [newTagName, setNewTagName] = useState('');
  
  const [formData, setFormData] = useState<CreateJokeRequest>({
    content: '',
    type: JokeTypeEnum.Joke,
    tags: [],
    laughScore: undefined,
    isActive: true,
  });

  const fetchTags = async () => {
    try {
      const tags = await apiService.getTags();
      setAvailableTags(tags.filter(tag => tag.isActive));
    } catch (error) {
      console.error('Failed to fetch tags:', error);
    }
  };

  useEffect(() => {
    fetchTags();
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!formData.content.trim()) {
      toast.error('Please enter joke content');
      return;
    }

    try {
      setLoading(true);
      await apiService.createJoke(formData);
      toast.success('Joke created successfully!');
      navigate('/jokes');
    } catch (error) {
      console.error('Failed to create joke:', error);
      toast.error('Failed to create joke. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleTagAdd = (tagName: string) => {
    if (!formData.tags?.includes(tagName)) {
      setFormData({
        ...formData,
        tags: [...(formData.tags || []), tagName],
      });
    }
  };

  const handleTagRemove = (tagName: string) => {
    setFormData({
      ...formData,
      tags: formData.tags?.filter(tag => tag !== tagName) || [],
    });
  };

  const handleCreateNewTag = async () => {
    if (!newTagName.trim()) return;
    
    try {
      const newTag = await apiService.createTag({ name: newTagName.trim() });
      setAvailableTags([...availableTags, newTag]);
      handleTagAdd(newTag.name);
      setNewTagName('');
      toast.success('Tag created and added!');
    } catch (error) {
      console.error('Failed to create tag:', error);
      toast.error('Failed to create tag');
    }
  };

  return (
    <PageLayout 
      maxWidth="2xl"
      title="Create New Joke"
      subtitle="Share your humor with the world!"
    >
      <form onSubmit={handleSubmit} className="space-y-6">
          {/* Joke Content */}
          <div>
            <label htmlFor="content" className="block text-sm font-medium text-gray-700 mb-2">
              Joke Content *
            </label>
            <textarea
              id="content"
              value={formData.content}
              onChange={(e) => setFormData({ ...formData, content: e.target.value })}
              rows={5}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="Enter your hilarious joke here..."
              required
            />
          </div>

          {/* Joke Type */}
          <div>
            <label htmlFor="type" className="block text-sm font-medium text-gray-700 mb-2">
              Joke Type
            </label>
            <select
              id="type"
              value={formData.type}
              onChange={(e) => setFormData({ ...formData, type: parseInt(e.target.value) as JokeType })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              <option value={JokeTypeEnum.Joke}>Joke</option>
              <option value={JokeTypeEnum.FunnySaying}>Funny Saying</option>
              <option value={JokeTypeEnum.Discouragement}>Discouragement</option>
              <option value={JokeTypeEnum.SelfDeprecating}>Self Deprecating</option>
              <option value={JokeTypeEnum.Personal}>Personal</option>
              <option value={JokeTypeEnum.UserSubmission}>User Submission</option>
              <option value={JokeTypeEnum.Api}>API</option>
              <option value={JokeTypeEnum.ThirdParty}>Third Party</option>
            </select>
          </div>

          {/* Laugh Score */}
          <div>
            <label htmlFor="laughScore" className="block text-sm font-medium text-gray-700 mb-2">
              Laugh Score (1-10)
            </label>
            <input
              type="number"
              id="laughScore"
              min="1"
              max="10"
              value={formData.laughScore || ''}
              onChange={(e) => setFormData({ 
                ...formData, 
                laughScore: e.target.value ? parseInt(e.target.value) : undefined 
              })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="Rate your joke (optional)"
            />
          </div>

          {/* Tags Section */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Tags
            </label>
            
            {/* Selected Tags */}
            {formData.tags && formData.tags.length > 0 && (
              <div className="mb-3">
                <p className="text-sm text-gray-600 mb-2">Selected tags:</p>
                <div className="flex flex-wrap gap-2">
                  {formData.tags.map((tag) => (
                    <span
                      key={tag}
                      className="inline-flex items-center bg-blue-100 text-blue-800 px-2 py-1 rounded-md text-sm"
                    >
                      #{tag}
                      <button
                        type="button"
                        onClick={() => handleTagRemove(tag)}
                        className="ml-1 text-blue-600 hover:text-blue-800"
                        title={`Remove ${tag} tag`}
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </span>
                  ))}
                </div>
              </div>
            )}

            {/* Available Tags */}
            {availableTags.length > 0 && (
              <div className="mb-3">
                <p className="text-sm text-gray-600 mb-2">Available tags:</p>
                <div className="flex flex-wrap gap-2">
                  {availableTags
                    .filter(tag => !formData.tags?.includes(tag.name))
                    .map((tag) => (
                      <button
                        key={tag.id}
                        type="button"
                        onClick={() => handleTagAdd(tag.name)}
                        className="inline-flex items-center bg-gray-100 text-gray-700 px-2 py-1 rounded-md text-sm hover:bg-gray-200 transition-colors"
                      >
                        <Plus className="h-3 w-3 mr-1" />
                        #{tag.name}
                      </button>
                    ))}
                </div>
              </div>
            )}

            {/* Create New Tag */}
            <div className="flex gap-2">
              <input
                type="text"
                value={newTagName}
                onChange={(e) => setNewTagName(e.target.value)}
                placeholder="Create new tag..."
                className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent text-sm"
              />
              <button
                type="button"
                onClick={handleCreateNewTag}
                disabled={!newTagName.trim()}
                className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors text-sm"
              >
                Create Tag
              </button>
            </div>
          </div>

          {/* Submit Button */}
          <div className="flex gap-4">
            <button
              type="submit"
              disabled={loading}
              className="flex-1 bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {loading ? 'Creating...' : 'Create Joke'}
            </button>
            <button
              type="button"
              onClick={() => navigate('/jokes')}
              className="px-6 py-2 border border-gray-300 text-gray-700 rounded-md hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
          </div>
      </form>
    </PageLayout>
  );
};

export default CreateJokePage; 