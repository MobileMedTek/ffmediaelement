﻿namespace Unosquare.FFME.Decoding
{
    using Core;
    using FFmpeg.AutoGen;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a wrapper for an unmanaged Subtitle frame.
    /// TODO: Only text subtitles are supported currently
    /// </summary>
    /// <seealso cref="Unosquare.FFME.Core.MediaFrame" />
    internal unsafe sealed class SubtitleFrame : MediaFrame
    {
        #region Private Members

        private AVSubtitle* m_Pointer = null;
        private bool IsDisposed = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleFrame" /> class.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        internal SubtitleFrame(AVSubtitle* frame, MediaComponent component)
            : base(frame, component)
        {
            m_Pointer = (AVSubtitle*)InternalPointer;

            // Extract timing information (pts for Subtitles is always in AV_TIME_BASE units)
            var timeOffset = TimeSpan.FromTicks(frame->pts.ToTimeSpan(ffmpeg.AV_TIME_BASE).Ticks - component.Container.MediaStartTimeOffset.Ticks);
            StartTime = TimeSpan.FromTicks(timeOffset.Ticks + ((long)frame->start_display_time).ToTimeSpan(StreamTimeBase).Ticks);
            EndTime = TimeSpan.FromTicks(timeOffset.Ticks + ((long)frame->end_display_time).ToTimeSpan(StreamTimeBase).Ticks);
            Duration = TimeSpan.FromTicks(EndTime.Ticks - StartTime.Ticks);

            // Extract text strings
            for (var i = 0; i < frame->num_rects; i++)
            {
                var rect = frame->rects[i];
                if (rect->text != null)
                    Text.Add(Utils.PtrToStringUTF8(rect->text));
            }

            // Immediately release the frame as the struct was created in managed memory
            // Accessing it later will eventually caused a memory access error.
            if (m_Pointer != null)
                ffmpeg.avsubtitle_free(m_Pointer);

            m_Pointer = null;
            InternalPointer = null;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        public override MediaType MediaType => MediaType.Subtitle;

        /// <summary>
        /// Gets the pointer to the unmanaged subtitle struct
        /// </summary>
        internal AVSubtitle* Pointer { get { return m_Pointer; } }

        /// <summary>
        /// Gets lines of text that the subtitle frame contains.
        /// </summary>
        public List<string> Text { get; } = new List<string>(16);

        #endregion


        #region IDisposable Support

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (m_Pointer != null)
                    ffmpeg.avsubtitle_free(m_Pointer);
                m_Pointer = null;
                InternalPointer = null;
                IsDisposed = true;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SubtitleFrame"/> class.
        /// </summary>
        ~SubtitleFrame()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
