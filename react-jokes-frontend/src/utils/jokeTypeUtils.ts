import { JokeType } from '../types';

export const getJokeTypeLabel = (type: JokeType): string => {
  switch (type) {
    case JokeType.Default:
      return 'Default';
    case JokeType.Personal:
      return 'Personal';
    case JokeType.Api:
      return 'API';
    case JokeType.UserSubmission:
      return 'User Submission';
    case JokeType.ThirdParty:
      return 'Third Party';
    case JokeType.SocialMedia:
      return 'Social Media';
    case JokeType.Unknown:
      return 'Unknown';
    case JokeType.Joke:
      return 'Joke';
    case JokeType.FunnySaying:
      return 'Funny Saying';
    case JokeType.Discouragement:
      return 'Discouragement';
    case JokeType.SelfDeprecating:
      return 'Self Deprecating';
    default:
      return 'Unknown';
  }
};

export const getJokeTypeOptions = () => [
  { value: JokeType.Joke, label: 'Joke' },
  { value: JokeType.FunnySaying, label: 'Funny Saying' },
  { value: JokeType.Discouragement, label: 'Discouragement' },
  { value: JokeType.SelfDeprecating, label: 'Self Deprecating' },
  { value: JokeType.Personal, label: 'Personal' },
  { value: JokeType.UserSubmission, label: 'User Submission' },
  { value: JokeType.Api, label: 'API' },
  { value: JokeType.ThirdParty, label: 'Third Party' },
  { value: JokeType.SocialMedia, label: 'Social Media' },
  { value: JokeType.Unknown, label: 'Unknown' },
  { value: JokeType.Default, label: 'Default' },
]; 