﻿using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Layer of test merge data required internally
	/// </summary>
	public abstract class TestMergeBase : TestMergeParameters
	{
		/// <summary>
		/// The title of the pull request
		/// </summary>
		[Required]
		public string? TitleAtMerge { get; set; }

		/// <summary>
		/// The body of the pull request
		/// </summary>
		[Required]
		public string? BodyAtMerge { get; set; }

		/// <summary>
		/// The URL of the pull request
		/// </summary>
		[Required]
#pragma warning disable CA1056 // Uri properties should not be strings
		public string? Url { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings

		/// <summary>
		/// The author of the pull request
		/// </summary>
		[Required]
		public string? Author { get; set; }

		/// <summary>
		/// Construct a <see cref="TestMergeBase"/>
		/// </summary>
		protected TestMergeBase() { }

		/// <summary>
		/// Construct a <see cref="TestMergeBase"/> from a <paramref name="copy"/>
		/// </summary>
		/// <param name="copy">The <see cref="TestMergeBase"/> to copy data from</param>
		protected TestMergeBase(TestMergeBase copy)
		{
			if (copy == null)
				throw new ArgumentNullException(nameof(copy));
			Author = copy.Author;
			BodyAtMerge = copy.BodyAtMerge;
			Comment = copy.Comment;
			Number = copy.Number;
			PullRequestRevision = copy.PullRequestRevision;
			TitleAtMerge = copy.TitleAtMerge;
			Url = copy.Url;
		}
	}
}
