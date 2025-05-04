using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using System.Net;
using System.IO;


namespace MovieBrowserApp
{
    public partial class Form1 : Form
    {
        private const string TmdbApiKey = "YOUR_TMDB_API_KEY";
        private const string TmdbApiBaseUrl = "https://api.themoviedb.org/3/";
        private const string TmdbImageBaseUrl = "https://image.tmdb.org/t/p/w500";

        private static readonly HttpClient client = new HttpClient();


        public Form1()
        {
            InitializeComponent();
            lstResults.DisplayMember = "Title";
            lstResults.ValueMember = "Id";
        }

        public class MovieSearchResult
        {
            public int Id { get; set; }
            public string Title { get; set; }
            [JsonPropertyName("release_date")]
            public string ReleaseDate { get; set; }
            [JsonPropertyName("poster_path")]
            public string PosterPath { get; set; }
            public string Overview { get; set; }
        }

        public class MovieSearchResponse
        {
            public int Page { get; set; }
            public List<MovieSearchResult> Results { get; set; }
            [JsonPropertyName("total_pages")]
            public int TotalPages { get; set; }
            [JsonPropertyName("total_results")]
            public int TotalResults { get; set; }
        }

        public class MovieDetails
        {
            public int Id { get; set; }
            public string Title { get; set; }
            [JsonPropertyName("release_date")]
            public string ReleaseDate { get; set; }
             [JsonPropertyName("poster_path")]
            public string PosterPath { get; set; }
            public string Overview { get; set; }
            public double VoteAverage { get; set; }
        }

        private async Task<MovieSearchResponse> SearchMoviesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            string requestUrl = $"{TmdbApiBaseUrl}search/movie?api_key={TmdbApiKey}&query={Uri.EscapeDataString(query)}";

            try
            {
                HttpResponseMessage response = await client.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();

                MovieSearchResponse searchResults = JsonSerializer.Deserialize<MovieSearchResponse>(jsonResponse);

                return searchResults;
            }
            catch (HttpRequestException httpEx)
            {
                MessageBox.Show($"HTTP Request Error: {httpEx.Message}\nCheck your API Key or network connection.", "API Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            catch (JsonException jsonEx)
            {
                MessageBox.Show($"JSON Parsing Error: {jsonEx.Message}\nUnexpected response format from API.", "API Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred during search: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private async Task<MovieDetails> GetMovieDetailsAsync(int movieId)
        {
             if (movieId <= 0)
            {
                return null;
            }

            string requestUrl = $"{TmdbApiBaseUrl}movie/{movieId}?api_key={TmdbApiKey}";

            try
            {
                HttpResponseMessage response = await client.GetAsync(requestUrl);
                 response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();

                MovieDetails movieDetails = JsonSerializer.Deserialize<MovieDetails>(jsonResponse);

                return movieDetails;
            }
             catch (HttpRequestException httpEx)
            {
                MessageBox.Show($"HTTP Request Error fetching details: {httpEx.Message}", "API Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            catch (JsonException jsonEx)
            {
                MessageBox.Show($"JSON Parsing Error fetching details: {jsonEx.Message}", "API Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred fetching details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

         private async Task<Image> LoadImageFromUrlAsync(string imageUrl)
        {
             if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            try
            {
                using (var http = new HttpClient())
                using (var response = await http.GetAsync(imageUrl))
                {
                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var memStream = new MemoryStream())
                        {
                            await stream.CopyToAsync(memStream);
                            memStream.Position = 0;
                            return Image.FromStream(memStream);
                        }
                    }
                }
            }
             catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
                return null;
            }
        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            string searchTerm = txtSearch.Text.Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("Please enter a movie title to search.", "Input Needed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            lstResults.Items.Clear();
            ClearMovieDetails();

            MovieSearchResponse results = await SearchMoviesAsync(searchTerm);

            if (results != null && results.Results != null)
            {
                if (results.Results.Count > 0)
                {
                    foreach (var movie in results.Results)
                    {
                        lstResults.Items.Add(movie);
                    }
                }
                else
                {
                    MessageBox.Show($"No movies found for '{searchTerm}'.", "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private async void lstResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstResults.SelectedItem != null)
            {
                MovieSearchResult selectedMovie = (MovieSearchResult)lstResults.SelectedItem;

                MovieDetails details = await GetMovieDetailsAsync(selectedMovie.Id);

                if (details != null)
                {
                    lblTitle.Text = details.Title;
                    lblReleaseDate.Text = $"Release Date: {details.ReleaseDate}";
                    lblOverview.Text = details.Overview;


                    if (!string.IsNullOrEmpty(details.PosterPath))
                    {
                        string imageUrl = $"{TmdbImageBaseUrl}{details.PosterPath}";
                        Image posterImage = await LoadImageFromUrlAsync(imageUrl);
                        if (posterImage != null)
                        {
                            picPoster.Image = posterImage;
                        }
                        else
                        {
                             picPoster.Image?.Dispose();
                             picPoster.Image = null;
                        }
                    }
                    else
                    {
                         picPoster.Image?.Dispose();
                         picPoster.Image = null;
                    }
                }
                else
                {
                     ClearMovieDetails();
                }
            }
            else
            {
                ClearMovieDetails();
            }
        }

        private void ClearMovieDetails()
        {
            lblTitle.Text = "";
            lblReleaseDate.Text = "";
            lblOverview.Text = "";
            picPoster.Image?.Dispose();
            picPoster.Image = null;
        }

         private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            picPoster.Image?.Dispose();
        }
    }
}
