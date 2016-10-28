﻿namespace Microsoft.Bot.Builder.Location.Bing
{
    using System.Threading.Tasks;

    internal interface IGeoSpatialService
    {
        /// <summary>
        /// Gets the locations asynchronously.
        /// </summary>
        /// <param name="address">The address query.</param>
        /// <returns>The found locations</returns>
        Task<LocationSet> GetLocationsByQueryAsync(string address);

        /// <summary>
        /// Gets the locations asynchronously.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <returns>The found locations</returns>
        Task<LocationSet> GetLocationsByPointAsync(double latitude, double longitude);

        /// <summary>
        /// Gets the map image URL.
        /// </summary>
        /// <param name="point">The geocode point.</param>
        /// <param name="index">The pin point index.</param>
        /// <returns></returns>
        string GetLocationMapImageUrl(GeocodePoint point, int? index = null);
    }
}