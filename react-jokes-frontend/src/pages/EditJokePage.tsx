import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Plus, X, ArrowLeft } from 'lucide-react';
import type { UpdateJokeRequest, Tag, JokeType, Joke } from '../types';
import { JokeType as JokeTypeEnum } from '../types';
import PageLayout from '../components/PageLayout';
import apiService from '../services/api';
import toast from 'react-hot-toast';

const EditJokePage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [loading, setLoading] = useState(false);
  const [initialLoading, setInitialLoading] = useState(true);
  const [availableTags, setAvailableTags] = useState<Tag[]>([]);
  const [newTagName, setNewTagName] = useState('');
  const [joke, setJoke] = useState<Joke | null>(null);
  
  const [formData, setFormData] = useState<UpdateJokeRequest>({
    content: '',
    type: JokeTypeEnum.Joke,
    tags: [],
    laughScore: undefined,
    isActive: true,
  });

  const fetchJoke = async () => {
    if (!id) {
      toast.error('No joke ID provided');
      navigate('/jokes');
      return;
    }

    try {
      setInitialLoading(true);
      const fetchedJoke = await apiService.getJoke(id);
      setJoke(fetchedJoke);
      setFormData({
        content: fetchedJoke.content,
        type: fetchedJoke.type,
        tags: fetchedJoke.tags.map(tag => tag.name),
        laughScore: fetchedJoke.laughScore,
        isActive: fetchedJoke.isActive,
      });
    } catch (error) {
      console.error('Failed to fetch joke:', error);
      toast.error('Failed to load joke');
      navigate('/jokes');
    } finally {
      setInitialLoading(false);
    }
  };

  const fetchTags = async () => {
    try {
      const tags = await apiService.getTags();
      setAvailableTags(tags.filter(tag => tag.isActive));
    } catch (error) {
      console.error('Failed to fetch tags:', error);
    }
  };

  useEffect(() => {
    fetchJoke();
    fetchTags();
  }, [id]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!formData.content.trim()) {
      toast.error('Please enter joke content');
      return;
    }

    if (!id) {
      toast.error('No joke ID provided');
      return;
    }

    try {
      setLoading(true);
      await apiService.updateJoke(id, formData);
      toast.success('Joke updated successfully!');
      navigate('/jokes');
    } catch (error) {
      console.error('Failed to update joke:', error);
      toast.error('Failed to update joke. Please try again.');
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

  if (initialLoading) {
    return (
      <div className="max-w-2xl mx-auto">
        <div className="text-center py-8">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading joke...</p>
        </div>
      </div>
    );
  }

  if (!joke) {
    return (
      <div className="max-w-2xl mx-auto">
        <div className="bg-white rounded-lg shadow-md p-6 text-center">
          <p className="text-gray-500 mb-4">Joke not found</p>
          <button
            onClick={() => navigate('/jokes')}
            className="inline-flex items-center space-x-2 bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors"
          >
            <ArrowLeft className="h-4 w-4" />
            <span>Back to Jokes</span>
          </button>
        </div>
      </div>
    );
  }

  return (
    <PageLayout 
      maxWidth="2xl"
      title="Edit Joke"
      subtitle="Update your humor and make it even better!"
    >
      <div className="mb-6">
        <button
          onClick={() => navigate('/jokes')}
          className="flex items-center space-x-2 text-gray-600 hover:text-gray-800 transition-colors"
        >
          <ArrowLeft className="h-4 w-4" />
          <span>Back to Jokes</span>
        </button>
      </div>
      
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

          {/* Active Status */}
          <div>
            <label className="flex items-center space-x-2">
              <input
                type="checkbox"
                checked={formData.isActive}
                onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                className="rounded border-gray-300 focus:ring-2 focus:ring-blue-500"
              />
              <span className="text-sm font-medium text-gray-700">Active</span>
            </label>
            <p className="text-xs text-gray-500 mt-1">Inactive jokes won't be visible to other users</p>
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

          {/* Submit Buttons */}
          <div className="flex gap-4">
            <button
              type="submit"
              disabled={loading}
              className="flex-1 bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {loading ? 'Updating...' : 'Update Joke'}
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

export default EditJokePage; 