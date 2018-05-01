"""Convenience methods to preprocess sound data into formant stuff"""

from collections import namedtuple
import numpy as np
import torch
from torch.utils.data import Dataset
from torch.utils.data.sampler import WeightedRandomSampler
import scipy.io.wavfile as wavfile

SoundData = namedtuple('SoundData', ['rate', 'data', 'binwidth', 'sample_rate'])
AnimationData = namedtuple('AnimationData', ['rate', 'data', 'mean', 'std', 'attribs'])
PhonemeData = namedtuple('PhonemeData', ['data'])

class SoundDataset(Dataset):
    """Dataset that handles a sound file.

    Arguments:
    sound_file: Path to the .wav sound file. The sound file
    should be a 16kHz, uncompressed, mono .wav.
    animation_rate: Rate of the animation in Hz.
    halfwidth: Half the width of the sound bins.
    formants: Number of formants to use in the sound analysis.
    bins: How many bins to use for the sound window.
    drop: How many seconds to drop at the beginning of the recording.
    """

    def __init__(self, sound_file, animation_rate, halfwidth=128, formants=32, bins=64, drop=0.,
                 leadin=0.0, leadout=0.0):
        self.halfwidth = halfwidth
        self.bins = bins
        self.formants = formants
        self.animation_rate = animation_rate
        self.sound = load_sound(
        sound_file, halfwidth, formants, bins, drop, pad_beginning=True, pad_end=True,
        leadin=leadin, leadout=leadout)
        self.anim_to_snd_ratio = self.sound.rate / animation_rate

    def __getitem__(self, index):
        sound_idx = int(self.anim_to_snd_ratio * index)
        try:
            ret = self.sound.data[:, sound_idx:sound_idx+self.bins, :]
            if ret.size(1) < self.bins: #This should not happen because we pad in load_sound
                raise ValueError
        except ValueError:
            print("""
                sound index = {}
                index = {}
                sound.data.size() = {}
                len(self) = {}""".format(sound_idx, index, self.sound.data.size(), len(self)))
            raise
        return ret

    def __len__(self):
        num_frames = int(round(self.sound.data.size()[1]/self.anim_to_snd_ratio))
        window_half_len_frames = int(round(
        self.bins*self.halfwidth/self.sound.sample_rate * self.animation_rate))
        if num_frames > window_half_len_frames:
            return num_frames - window_half_len_frames
        else:
            return num_frames


def load_sound(sound_file, halfwidth, formants, bins, drop,
               pad_beginning=True, pad_end=True, leadin=0.0, leadout=0.0):
    """Creates a nicely formatted sample from a set of frames."""
    print("Loading sound ('load_sound'): " + sound_file)
    sample_rate, sound_data = wavfile.read(sound_file)
    sample_rate = float(sample_rate)

    # add lead-in/out
    sound_data = np.concatenate((np.zeros(int(leadin/30.0*sample_rate)), sound_data,
    np.zeros(int(leadout/30.0*sample_rate))))

    sound_data = np.roll(sound_data, -int(drop * sample_rate))
    padding = np.random.randn(halfwidth * bins // 2) * 0.0 # Noise as padding could mess up the lstm
    if pad_beginning:
        print("Pad beginning, frames: {} in s: {}".format(
        len(padding) / sample_rate * 30, len(padding) / sample_rate))
        sound_data = np.concatenate((padding, sound_data))
    if pad_end:
        print("Pad end, frames: {} in s: {}".format(
        len(padding) / sample_rate * 30, len(padding) / sample_rate))
        sound_data = np.concatenate((sound_data, padding))
    sound_data = sound_data / max(abs(sound_data)) # NEW: Normalize between [-1, 1]
    hann_filter = np.hanning(2 * halfwidth)

    def get_all_frames():
        """Get a self.bins * halfwidth samples wide window."""
        bins = int(sound_data.shape[0]/halfwidth)-1
        for i in range(bins):
            s = halfwidth*i
            e = halfwidth*(i+2)
            r = sound_data[s:e]
            yield r

    d = 2*halfwidth * np.ones(4 * halfwidth - 1)
    def standard_form(sample):
        """Apply hann, subtract mean, autocorrelate."""
        hanned = hann_filter * sample
        # Autocorrelation code from stattools.py#417
        no_dc = hanned - hanned.mean()
        acov = (np.correlate(no_dc, no_dc, 'full') / d)[2 * halfwidth - 1:]
        acf = acov[:formants + 1] / (acov[0]+1e-200)
        return acf

    raw_samples = [standard_form(f) for f in get_all_frames()]
    array = np.array(raw_samples)[:, 0:formants]
    array = np.reshape(array, (1, -1, formants))
    total_time = sound_data.shape[0]/sample_rate
    bin_rate = array.shape[1] / total_time

    print("Sound time: ", total_time, ", length: ", array.shape[1], ", bin rate: ", bin_rate)

    return SoundData(bin_rate, torch.Tensor(array), halfwidth, sample_rate)
