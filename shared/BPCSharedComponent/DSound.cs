/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using SharpDX.X3DAudio;
using BPCSharedComponent.Security;


namespace BPCSharedComponent.ExtendedAudio
{
	public class DSound
	{
		private static String rootDir;
		private static Object playLock;
		private static string pass;

		public static string SFileName;
		//used to store all sounds for cleanup
		public static ArrayList Sounds = new ArrayList();
		//main soundcard object
		private static XAudio2 mainSoundDevice;
		private static MasteringVoice mainMasteringVoice;
		private static Listener listener;
		private static XAudio2 musicDevice;
		private static MasteringVoice musicMasteringVoice;
		public static float masterMusicVolume;
		//the listener used for 3d sound
		//used to hold sounds path
		public static string SoundPath;
		//used to hold narratives
		public static string NSoundPath;
		//used to hold numbers
		public static string NumPath;

		/// <summary>
		/// Initializes the sound library for playback.
		/// </summary>
		/// <param name="root">The root directory of the sounds.</param>
		public static void initialize(String root)
		{
			setRootDirectory(root);
			SoundPath = "s";
			NSoundPath = SoundPath + "\\n";
			NumPath = NSoundPath + "\\ns";
			mainSoundDevice = new XAudio2();
			mainMasteringVoice = new MasteringVoice(mainSoundDevice);
			musicDevice = new XAudio2();
			musicMasteringVoice = new MasteringVoice(musicMasteringVoice);
			//get the listener:
			setListener();
		}

		/// <summary>
		/// Loads a wave file into a SourceVoice.
		/// </summary>
		/// <param name="FileName">The path of the file to load.</param>
		/// <returns>A populated ExtendedAudioBuffer.</returns>
		public static ExtendedAudioBuffer LoadSound(string FileName)
		{
			if (!File.Exists(FileName)) {
				throw (new ArgumentException("The sound " + FileName + " could not be found."));
			}
			SoundStream stream = new SoundStream(File.OpenRead(FileName));
			WaveFormat format = stream.Format; // So we don't lose reference to it when we close the stream.
			AudioBuffer buffer = new AudioBuffer { Stream = stream.ToDataStream(), AudioBytes = (int)stream.Length, Flags = SharpDX.XAudio2.BufferFlags.EndOfStream };
			// We can now safely close the stream.
			stream.Close();
			SourceVoice sv = new SourceVoice(mainSoundDevice, format, true);
			return new ExtendedAudioBuffer(buffer, sv);
		}

		/// <summary>
		/// Creates a new listener object with all of its values set to the default unit vectors per the documentation.
		/// </summary>
		public static void setListener()
		{
			listener = new Listener
			{
				OrientFront = new Vector3(0, 0, 1),
				OrientTop = new Vector3(0, 1, 0),
				Position = new Vector3(0, 0, 0),
				Velocity = new Vector3(0, 0, 0)
			};
		}

		/// <summary>
		/// Orients the listener. The x, y and z values are the respective components of the front and top vectors of the listener. For instance, to orient the listener to its default orientation, one should call setOrientation(0,0,1,0,1,0), IE: the default orientation vectors.
		/// </summary>
		/// <param name="x1"></param>
		/// <param name="y1"></param>
		/// <param name="z1"></param>
		/// <param name="x2"></param>
		/// <param name="y2"></param>
		/// <param name="z2"></param>
		public static void setOrientation(double x1, double y1, double z1, double x2, double y2, double z2)
		{
			Vector3 front = new Vector3((float)x1, (float)y1, (float)z1);
			Vector3 top = new Vector3((float)x2, (float)y2, (float)z2);
			listener.FrontOrientation = front;
			listener.TopOrientation = top;
		}

		/// <summary>
		/// Plays a sound.
		/// </summary>
		/// <param name="sound">The ExtendedAudioBuffer to play.</param>
		/// <param name="stop">If true, will stop the sound and return its position to 0 before playing it. Passing false will have the effect of resuming the sound from the last position it was stopped at.</param>
		/// <param name="loop">Whether or not to loop the sound.</param>
		public static void PlaySound(ExtendedAudioBuffer sound, bool stop, bool loop)
		{
			sound.play(stop, loop);
		}

		/// <summary>
		/// Positions a sound in 3-D space
		/// </summary>
		/// <param name="sound">The ExtendedAudioBuffer to play.</param>
		/// <param name="stop">If true, will stop the sound and return its position to 0 before playing it. Passing false will have the effect of resuming the sound from the last position it was stopped at.</param>
		/// <param name="loop">Whether or not to loop the sound.</param>
		/// <param name="x">The x coordinate of the source.</param>
		/// <param name="y">The y coordinate of the source.</param>
		/// <param name="z">The z coordinate of the source.</param>
		public static void PlaySound3d(ExtendedAudioBuffer sound, bool stopt, bool loop, double x, double y, double z)
		{
			X3DAudio x3dAudio = new X3DAudio(Speakers.FrontRight);
			Emitter emitter = new Emitter {
				ChannelCount = 1,
				CurveDistanceScaler = float.MinValue,
				OrientFront = new Vector3(0, 0, 1),
				OrientTop = new Vector3(0, 1, 0),
				Position = new Vector3(x,y,z)
			};
			sound.play(stop, loop);
			DspSettings dspSettings = x3dAudio.Calculate(listener, emitter, CalculateFlags.Matrix | CalculateFlags.Doppler, 1, 2);
			sound.apply3d(dspSettings);
		}

		/// <summary>
		/// Sets the position of the listener.
		/// </summary>
		/// <param name="x">The x coordinate of the listener.</param>
		/// <param name="y">The y coordinate of the listener.</param>
		/// <param name="z">The z coordinate of the listener.</param>
		public static void SetCoordinates(double x, double y, double z)
		{
			listener.Position = new Vector3((float)x, (float)y, (float)z);
		}

		/// <summary>
		/// Loads an ogg file into memory.
		/// </summary>
		/// <param name="fileName">The file name.</param>
		/// <param name="v">The starting volume.</param>
		/// <returns>An ogg buffer ready to be played.</returns>
		public static OggBuffer loadOgg(string fileName, float v)
		{
			if (!File.Exists(fileName))
				throw (new ArgumentException("The sound " + fileName + " could not be found."));
			return new OggBuffer(fileName, v, musicDevice);
		}

		/// <summary>
		/// Loads an ogg file into memory, with maximum volume.
		/// </summary>
		/// <param name="fileName">The file name.</param>
		/// <returns>An ogg buffer ready to be played.</returns>
		public static OggBuffer loadOgg(string fileName)
		{
			return loadOgg(fileName, 1.0f);
		}

		/// <summary>
		/// Used to create a playing chain. The last files will be looped indefinitely and the files before it will only play once, in order.
		/// </summary>
		/// <param name="v">The starting volume.</param>
		/// <param name="fileNames">A list of file names to play, where the last one is looped indefinitely.</param>
		/// <returns>An ogg buffer that is ready to be played.</returns>
		public static OggBuffer loadOgg(float v, params string[] fileNames)
		{
			for (int i = 0; i < fileNames.Length; i++) {
				if (!File.Exists(fileNames[i]))
					throw (new ArgumentException("The sound " + fileNames[i] + " could not be found."));
			}
			return new OggBuffer(fileNames, v, musicDevice);
		}

		/// <summary>
		/// Unloads the sound from memory. The memory will be freed and the object reference will be set to NULL. The sound will also be stopped if it is playing.
		/// </summary>
		/// <param name="sound">The sound to unload.</param>
		[MethodImplAttribute(MethodImplOptions.Synchronized)]
		public static void unloadSound(ref ExtendedAudioBuffer sound)
		{
			if (sound == null || sound.IsDisposed) {
				sound = null;
				return;
			}
			sound.Stop();
			sound.Dispose();
			sound = null;
		}

		/// <summary>
		///  Checks to see if a sound is playing.
		/// </summary>
		/// <param name="s">The sound to check</param>
		/// <returns>True if the sound is playing, false if either s is NULL or is not playing.</returns>
		public static bool isPlaying(SecondarySoundBuffer s)
		{
			if (s == null || s.IsDisposed)
				return false;
			if ((s.Status & (int)BufferStatus.Playing) == (int)BufferStatus.Playing)
				return true;
			return false;
		}

		public static bool isLooping(SecondarySoundBuffer s)
		{
			if (s == null || s.IsDisposed)
				return false;
			if ((s.Status & (int)BufferStatus.Looping) == (int)BufferStatus.Looping)
				return true;
			return false;
		}

		public static void getWaveData(string filename)
		{
			FileStream s = new FileStream(filename, FileMode.Open,
				 FileAccess.Read, FileShare.Read);

		}

		/// <summary>
		///  Loads and plays the specified wave file, and disposes it after it is done playing.
		/// </summary>
		/// <param name="fn">The name of the file to play.</param>
		public static void playAndWait(String fn)
		{
			SecondarySoundBuffer s = LoadSound(fn);
			PlaySound(s, true, false);
			while (isPlaying(s))
				Thread.Sleep(100);
			s.Dispose();
			s = null;
		}

		public static void cleanUp()
		{
			listener.Dispose();
			primaryBuffer.Dispose();
			objDS.Dispose();
		}

		/// <summary>
		/// Sets the root directory for sounds.
		/// </summary>
		/// <param name="root">The path of the root directory.</param>
		public static void setRootDirectory(String root)
		{
			rootDir = root;
		}

	}
}
