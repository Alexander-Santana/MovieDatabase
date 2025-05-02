using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json; // Requires .NET Core 3.1+ or .NET 5+
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing; // For handling images

// Add this namespace for handling images from URLs
using System.Net;
using System.IO;


namespace MovieBrowserApp // Replace with your project's namespace
{
    public partial class Form1 : Form // Make sure this matches your form class name
    {
        // --- Configuration ---
        private const string TmdbApiKey = "YOUR_TMDB_API_KEY"; // <<== REPLACE WITH YOUR KEY!
        private const string TmdbApiBaseUrl = "https://api.themoviedb.org/3/";
        private const string TmdbImageBaseUrl = "https://image.tmdb.org/t/p/w500"; // w500 is a common size


        // HttpClient should ideally be a single instance throughout your application's lifetime
        private static readonly HttpClient client = new HttpClient();


        public Form1()
        {
            InitializeComponent();
            // Optional: Configure the ListBox to store movie IDs while displaying titles
            lstResults.DisplayMember = "Title"; // Display the Title property
            lstResults.ValueMember = "Id";     // Store the Id property behind the scenes
        }

        // --- JSON Model Classes ---
        // These classes represent the structure of the JSON we expect from the API.
        // We only define the properties we need.

        // Model for a single movie search result
        public class MovieSearchResult
        {
            public int Id { get; set; } // Used for fetching details later
            public string Title { get; set; }
            [JsonPropertyName("release_date")] // Map "release_date" from JSON to ReleaseDate property
            public string ReleaseDate { get; set; }
            [JsonPropertyName("poster_path")] // Map "poster_path" for image URL
            public string PosterPath { get; set; }
            public string Overview { get; set; } // Synopsis
        }

        // Model for the overall search results response
        public class MovieSearchResponse
        {
            public int Page { get; set; }
            public List<MovieSearchResult> Results { get; set; }
            [JsonPropertyName("total_pages")]
            public int TotalPages { get; set; }
            [JsonPropertyName("total_results")]
            public int TotalResults { get; set; }
        }

        // Model for movie details response
        public class MovieDetails
        {
            public int Id { get; set; }
            public string Title { get; set; }
            [JsonPropertyName("release_date")]
            public string ReleaseDate { get; set; }
             [JsonPropertyName("poster_path")]
            public string PosterPath { get; set; }
            public string Overview { get; set; }
            public double VoteAverage { get; set; } // Example of another field
            // Add other fields you want to display (genres, runtime, etc.)
            // public List<Genre> Genres { get; set; } // Would need a Genre class
        }

        // --- API Call Methods ---

        // Asynchronous method to search for movies
        private async Task<MovieSearchResponse> SearchMoviesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null; // Don't search for empty queries
            }

            string requestUrl = $"{TmdbApiBaseUrl}search/movie?api_key={TmdbApiKey}&query={Uri.EscapeDataString(query)}";

            try
            {
                HttpResponseMessage response = await client.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode(); // Throw if not a success code (200-299)

                string jsonResponse = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON response into our C# object structure
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

        // Asynchronous method to get movie details by ID
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
                 response.EnsureSuccessStatusCode(); // Throw if not a success code

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

        // Asynchronous method to load image from URL
         private async Task<Image> LoadImageFromUrlAsync(string imageUrl)
        {
             if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            try
            {
                using (var http = new HttpClient()) // Use a local HttpClient for images or the shared one
                using (var response = await http.GetAsync(imageUrl))
                {
                    response.EnsureSuccessStatusCode(); // Throw if not success

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        // Creating image from stream directly can lock the file,
                        // so load it into memory first.
                        using (var memStream = new MemoryStream())
                        {
                            await stream.CopyToAsync(memStream);
                            memStream.Position = 0; // Reset stream position
                            return Image.FromStream(memStream);
                        }
                    }
                }
            }
             catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
                 // You might want to display a default "image not found" image
                return null;
            }
        }


        // --- Event Handlers (Connect these to your UI elements) ---

        // Example: Connect this method to your Search Button's Click event
        private async void btnSearch_Click(object sender, EventArgs e)
        {
            string searchTerm = txtSearch.Text.Trim(); // Get text from your search TextBox

            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("Please enter a movie title to search.", "Input Needed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Clear previous results and details
            lstResults.Items.Clear();
            ClearMovieDetails(); // Implement a method to clear detail labels/picturebox

            // Perform the search asynchronously
            MovieSearchResponse results = await SearchMoviesAsync(searchTerm);

            // Update the UI with results
            if (results != null && results.Results != null)
            {
                if (results.Results.Count > 0)
                {
                    // Add results to your ListBox
                    foreach (var movie in results.Results)
                    {
                         // Add the MovieSearchResult object itself if using DisplayMember/ValueMember
                        lstResults.Items.Add(movie);
                        // Or just add the title if not using DisplayMember/ValueMember: lstResults.Items.Add(movie.Title);
                    }
                }
                else
                {
                    MessageBox.Show($"No movies found for '{searchTerm}'.", "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // Example: Connect this method to your Results ListBox's SelectedIndexChanged event
        private async void lstResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstResults.SelectedItem != null)
            {
                // Assuming you added MovieSearchResult objects to the ListBox
                MovieSearchResult selectedMovie = (MovieSearchResult)lstResults.SelectedItem;

                // Fetch details for the selected movie asynchronously
                MovieDetails details = await GetMovieDetailsAsync(selectedMovie.Id);

                // Update UI with details
                if (details != null)
                {
                    lblTitle.Text = details.Title; // Update your Title Label
                    lblReleaseDate.Text = $"Release Date: {details.ReleaseDate}"; // Update Release Date Label
                    lblOverview.Text = details.Overview; // Update Overview Label
                    // You might want to add word wrap for the overview label

                    // Load and display the poster image
                    if (!string.IsNullOrEmpty(details.PosterPath))
                    {
                        string imageUrl = $"{TmdbImageBaseUrl}{details.PosterPath}";
                        Image posterImage = await LoadImageFromUrlAsync(imageUrl);
                        if (posterImage != null)
                        {
                            picPoster.Image = posterImage; // Set your PictureBox image
                             // picPoster.SizeMode = PictureBoxSizeMode.Zoom; // Adjust size mode as needed
                        }
                        else
                        {
                             picPoster.Image = null; // Clear image if loading failed
                             // Optionally load a default "image not found" image
                        }
                    }
                    else
                    {
                         picPoster.Image = null; // Clear image if no poster path
                         // Optionally load a default "no poster" image
                    }
                }
                else
                {
                     ClearMovieDetails(); // Clear details if fetching failed
                }
            }
            else
            {
                // If nothing is selected, clear details
                ClearMovieDetails();
            }
        }

        // Helper method to clear the detail display area
        private void ClearMovieDetails()
        {
            lblTitle.Text = "";
            lblReleaseDate.Text = "";
            lblOverview.Text = "";
            picPoster.Image?.Dispose(); // Dispose previous image if any
            picPoster.Image = null;
            // Clear other detail labels as needed
        }

        // --- Optional: Dispose HttpClient on form closing ---
         private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Dispose HttpClient when the application exits
            // Note: For a truly long-running app or if using .NET Core 2.1 or earlier,
            // consider HttpClientFactory. In modern .NET (3.1+), a single static HttpClient is fine.
            // client.Dispose(); // You can uncomment this, but the static instance lives as long as the app domain.
            // Better to dispose images loaded into PictureBoxes
            picPoster.Image?.Dispose();
        }

    }
}
