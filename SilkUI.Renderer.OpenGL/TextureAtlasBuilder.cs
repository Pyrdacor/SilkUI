using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SilkUI.Renderer.OpenGL
{
    internal class TextureAtlasBuilder
    {
        private class ImageSorter : IComparer<ImageHandle>
        {
            public int Compare(ImageHandle lhs, ImageHandle rhs)
            {
                // This will sort the largest images to the front.
                // If sizes are equal sort by greater height.
                int lhsSize = lhs.Width * lhs.Height;
                int rhsSize = rhs.Width * rhs.Height;

                if (lhsSize == rhsSize)
                {
                    if (lhs.Height == rhs.Height)
                        return lhs.Index.CompareTo(rhs.Index);

                    return lhs.Height > rhs.Height ? -1 : 1;
                }

                return lhsSize > rhsSize ? -1 : 1;
            }
        }

        private enum ChunkSortCriteria
        {
            Width,
            Height,
            Size
        }

        private class ChunkSorter : IComparer<Rectangle>
        {
            private readonly ChunkSortCriteria _chunkSortCriteria;

            public ChunkSorter(ChunkSortCriteria criteria)
            {
                _chunkSortCriteria = criteria;
            }

            public int Compare(Rectangle lhs, Rectangle rhs)
            {
                return _chunkSortCriteria switch
                {
                    ChunkSortCriteria.Width => rhs.Width.CompareTo(lhs.Width),
                    ChunkSortCriteria.Height => rhs.Height.CompareTo(lhs.Height),
                    ChunkSortCriteria.Size => (rhs.Width * rhs.Height).CompareTo(lhs.Width * lhs.Height),
                    _ => throw new ArgumentException($"Invalid chunk sort criteria: {_chunkSortCriteria}")
                };
            }
        }

        private readonly SortedSet<ImageHandle> _images = new SortedSet<ImageHandle>(new ImageSorter());

        public void AddImage(ImageHandle image)
        {
            _images.Add(image);
        }

        private static void SplitChunk(Rectangle chunk, int atlasWidth, int atlasHeight,
            int removedWidth, int removedHeight, out Rectangle? splitChunk1, out Rectangle? splitChunk2)
        {
            splitChunk1 = null;
            splitChunk2 = null;

            if (removedWidth == chunk.Width)
            {
                if (removedHeight < chunk.Height)
                    splitChunk1 = new Rectangle(chunk.X, chunk.Y + removedHeight, chunk.Width, chunk.Height - removedHeight);
            }
            else if (removedHeight == chunk.Height)
            {
                splitChunk1 = new Rectangle(chunk.X + removedWidth, chunk.Y, chunk.Width - removedWidth, chunk.Height);
            }
            else
            {
                if (atlasHeight >= atlasWidth) // Prefer creating bigger chunks to the right.
                {
                    splitChunk1 = new Rectangle(chunk.X + removedWidth, chunk.Y, chunk.Width - removedWidth, chunk.Height);
                    splitChunk2 = new Rectangle(chunk.X, chunk.Y + removedHeight, chunk.Width - removedWidth, chunk.Height - removedHeight);
                }
                else // Prefer creating bigger chunks to the bottom.
                {
                    splitChunk1 = new Rectangle(chunk.X + removedWidth, chunk.Y, chunk.Width - removedWidth, removedHeight);
                    splitChunk2 = new Rectangle(chunk.X, chunk.Y + removedHeight, chunk.Width, chunk.Height - removedHeight);
                }                
            }
        }

        // TODO: limit to some max dimensions?
        public TextureAtlas Create()
        {
            Dictionary<ImageHandle, Point> imagePositions = new Dictionary<ImageHandle, Point>();
            SortedSet<Rectangle> emptyChunksByWidth = new SortedSet<Rectangle>(new ChunkSorter(ChunkSortCriteria.Width));
            SortedSet<Rectangle> emptyChunksByHeight = new SortedSet<Rectangle>(new ChunkSorter(ChunkSortCriteria.Height));
            SortedSet<Rectangle> emptyChunksBySize = new SortedSet<Rectangle>(new ChunkSorter(ChunkSortCriteria.Size));

            void AddEmptyChunk(Rectangle chunk)
            {
                emptyChunksByWidth.Add(chunk);
                emptyChunksByHeight.Add(chunk);
                emptyChunksBySize.Add(chunk);
            }

            void RemoveEmptyChunk(Rectangle chunk)
            {
                emptyChunksByWidth.Remove(chunk);
                emptyChunksByHeight.Remove(chunk);
                emptyChunksBySize.Remove(chunk);
            }

            ImageHandle PopLargestImage()
            {
                var image = _images.Min;
                _images.Remove(image);
                return image;
            }

            var image = PopLargestImage();
            int atlasWidth = image.Width;
            int atlasHeight = image.Height;
            imagePositions.Add(image, new Point(0, 0));

            while (_images.Count != 0)
            {
                image = PopLargestImage();
                Rectangle? bestFitEmptyChunk = null;

                if (emptyChunksBySize.Count > 0) // Do we have empty chunks?
                {
                    if (image.Width >= image.Height / 2) // landscape image
                    {
                        var widestEmptyChunk = emptyChunksByWidth.Min;

                        if (widestEmptyChunk.Width >= image.Width && widestEmptyChunk.Height >= image.Height)
                            bestFitEmptyChunk = widestEmptyChunk;
                    }
                    else if (image.Height >= image.Width / 2) // portrait image
                    {
                        var heighestEmptyChunk = emptyChunksByHeight.Min;

                        if (heighestEmptyChunk.Width >= image.Width && heighestEmptyChunk.Height >= image.Height)
                            bestFitEmptyChunk = heighestEmptyChunk;
                    }
                    else // squarish image
                    {
                        var largestEmptyChunk = emptyChunksBySize.Min;

                        if (largestEmptyChunk.Width >= image.Width && largestEmptyChunk.Height >= image.Height)
                            bestFitEmptyChunk = largestEmptyChunk;
                    }
                }

                if (bestFitEmptyChunk == null)
                {
                    // No suitable empty chunk found -> Resize atlas.

                    if (atlasWidth > atlasHeight) // Add to the bottom.
                    {
                        imagePositions.Add(image, new Point(0, atlasHeight));
                        if (image.Width < atlasWidth)
                            AddEmptyChunk(new Rectangle(image.Width, atlasHeight, atlasWidth - image.Width, image.Height));
                        atlasHeight += image.Height;
                    }
                    else // Add to the right.
                    {
                        imagePositions.Add(image, new Point(atlasWidth, 0));
                        if (image.Height < atlasHeight)
                            AddEmptyChunk(new Rectangle(atlasWidth, image.Height, image.Width, atlasHeight - image.Height));
                        atlasWidth += image.Width;
                    }
                }
                else
                {
                    imagePositions.Add(image, bestFitEmptyChunk.Value.Location);
                    SplitChunk(bestFitEmptyChunk.Value, atlasWidth, atlasHeight, image.Width, image.Height,
                        out Rectangle? splitChunk1, out Rectangle? splitChunk2);
                    RemoveEmptyChunk(bestFitEmptyChunk.Value);
                    if (splitChunk1 != null)
                        AddEmptyChunk(splitChunk1.Value);
                    if (splitChunk2 != null)
                        AddEmptyChunk(splitChunk2.Value);
                }
            }

            // Create the texture.
            MutableTexture texture = new MutableTexture(atlasWidth, atlasHeight);

            foreach (var imagePosition in imagePositions)
            {
                var img = imagePosition.Key;
                texture.AddSprite(imagePosition.Value, img.Data, img.Width, img.Height, img.BytesPerPixel == 1);
            }

            texture.Finish(0);

            return new TextureAtlas(texture, imagePositions.ToDictionary(p => p.Key.Index, p => p.Value));
        }
    }
}
