﻿using LetsGoSEA.WebSite.Models;
using Microsoft.AspNetCore.Hosting;
using shortid;
using shortid.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LetsGoSEA.WebSite.Services
{
    /// <summary>
    /// Mediates communication between a NeighborhoodsController and Neighborhoods Data  
    /// </summary>
    public class NeighborhoodService
    {
        // Constructor
        public NeighborhoodService(IWebHostEnvironment webHostEnvironment)
        {
            WebHostEnvironment = webHostEnvironment;
        }

        // Getter: Get JSON file from wwwroot
        private IWebHostEnvironment WebHostEnvironment { get; }

        // Store the path of Neighborhoods JSON file (combine the root path, folder name, and file name)
        private string NeighborhoodFileName => Path.Combine(WebHostEnvironment.WebRootPath, "data", "neighborhoods.json");

        // Generate/retrieve a list of NeighborhoodModel objects from JSON file
        public IEnumerable<NeighborhoodModel> GetNeighborhoods()
        {
            // Open Neighborhoods JSON file
            using var jsonFileReader = File.OpenText(NeighborhoodFileName);

            // Read and Deserialize JSON file into an array of NeighborhoodModel objects
            return JsonSerializer.Deserialize<NeighborhoodModel[]>(jsonFileReader.ReadToEnd(),
                // Make case insensitive
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        /// <summary>
        /// Returns null if passed invalid id.
        /// Returns a single neighborhood corresponding to the id
        /// </summary>
        /// <param name="id">id of the requested neighborhood</param>
        /// <returns>NeighborhoodModel of the requested neighborhood</returns>
        public NeighborhoodModel GetNeighborhoodById(int? id)
        {
            try
            {
                var data = GetNeighborhoods().Where(x => x.Id == id);
                NeighborhoodModel singleNeighborhood = data.ElementAt(0);
                return singleNeighborhood;
            }
            catch (ArgumentOutOfRangeException)
            {
                // If the id passed is invalid, we return null
                return null;
            }
        }

        /// <summary>
        /// Save All neighborhood data to storage
        /// </summary>
        /// <param name="neighborhoods">all the neighborhood objects to be saved</param>
        private void SaveData(IEnumerable<NeighborhoodModel> neighborhoods)
        {
            // Re-write all the neighborhood data to JSON file
            using (var outputStream = File.Create(NeighborhoodFileName))
            {
                JsonSerializer.Serialize<IEnumerable<NeighborhoodModel>>(
                    new Utf8JsonWriter(outputStream, new JsonWriterOptions
                    {
                        SkipValidation = true,
                        Indented = true
                    }),
                    neighborhoods
                );
            }
        }

        /// <summary>
        /// Create a new neighborhood with autogenerated ID
        /// </summary>
        /// <returns>A NeighborhoodModel object to be used temporary in Page</returns>
        public NeighborhoodModel CreateID()
        {
            // Create a New Neighborhood 
            var data = new NeighborhoodModel()
            {
                // Generate the next valid Id number
                Id = GetNeighborhoods().Count() + 1,

                Name = "",
                Image = "",
                City = "Seattle",
                State = "WA",
                ShortDesc = ""
            };

            return data;
        }

        /// <summary>
        /// Create a new neighborhood object, add user input data to it, and save object in JSON file
        /// </summary>
        /// <param name="ID">ID generated in CreateID() method</param>
        /// <param name="name">name data entered by user</param>
        /// <param name="image">image data entered by user</param>
        /// <param name="shortDesc">short description entered by user</param>
        /// <returns>A new NeighborhoodModel object to be later saved in JSON</returns>
        public NeighborhoodModel AddData(string name, string image, string shortDesc)
        {
            // Create a new neighborhood model
            var data = new NeighborhoodModel()
            {
                // Add user input data to the corresponding field
                Id = GetNeighborhoods().Count() + 1,
                Name = name,
                Image = image,
                City = "Seattle",
                State = "WA",
                ShortDesc = shortDesc
            };

            // Get the current set, and append the new record to it 
            var dataset = GetNeighborhoods();
            dataset = dataset.Append(data);

            // Save data set in JSON
            SaveData(dataset);

            return data;
        }

        /// </summary>
        ///<param name="data">neighborhood data to be saved</param>
        ///<summary>
        /// Finds neighborhood in NeighborhoodModel, updates the neighborhood, and saves the Neighborhood
        /// </summary>
        ///<param name="data">neighborhood data to be saved</param>
        public NeighborhoodModel UpdateData(NeighborhoodModel data)
        {
            var neighborhoods = GetNeighborhoods();
            var neighborhoodData = neighborhoods.FirstOrDefault(x => x.Id.Equals(data.Id));
            if (neighborhoodData == null)
            {
                return null;
            }

            neighborhoodData.Name = data.Name;
            neighborhoodData.Image = data.Image;
            neighborhoodData.City = data.City;
            neighborhoodData.State = data.State;
            neighborhoodData.ShortDesc = data.ShortDesc;
            neighborhoodData.Ratings = data.Ratings;
            neighborhoodData.Comments = data.Comments;

            SaveData(neighborhoods);

            return neighborhoodData;
        }

        /// <summary>
        /// Remove the neighborhood record from the system
        /// </summary>
        /// <param name="id">id of the neighborhood to NOT be saved</param>
        /// <returns>the neighborhood object to be deleted</returns>
        public NeighborhoodModel DeleteData(int id)
        {
            // Get the current set
            var dataSet = GetNeighborhoods();

            // Get the record to be deleted
            var data = dataSet.FirstOrDefault(m => m.Id == id);

            // Only save the remaining records in the system
            var newDataSet = GetNeighborhoods().Where(m => m.Id != id);
            SaveData(newDataSet);

            // Return the record to be deleted
            return data;
        }

        /// <summary>
        /// Take in the neighborhood ID and the rating
        /// If the rating does not exist, add it
        /// Save the update
        /// </summary>
        /// <param name="neighborhood">Neighborhood Model to add rating to</param>
        /// <param name="rating">user input rating</param>
        public bool AddRating(NeighborhoodModel neighborhood, int rating)
        {
            // If neighborhood is null, return
            if (neighborhood == null)
            {
                return false;
            }

            // Check Rating for boundries, do not allow ratings below 0
            if (rating < 0)
            {
                return false;
            }

            // Check Rating for boundries, do not allow ratings above 5
            if (rating > 5)
            {
                return false;
            }

            // Check to see if ratings exist, if there are not, then create the array
            if (neighborhood.Ratings == null)
            {
                neighborhood.Ratings = new int[] { };
            }

            // Add the Rating to the Array
            var ratings = neighborhood.Ratings.ToList();
            ratings.Add(rating);
            neighborhood.Ratings = ratings.ToArray();


            // Save the data back to the data store
            UpdateData(neighborhood);

            return true;
        }

        /// <summary>
        /// Generates a new unique identifier
        /// </summary>
        /// <returns>String version of GUID</returns>
        public string CreateNewCommentId()
        {
            var options = new GenerationOptions(useNumbers: true, length:8);
            return ShortId.Generate(options).ToString();
        }


        /// <summary>
        /// Adds a comment to the NeighborhoodModel.
        /// </summary>
        /// <param name="neighborhood">Current neighborhood</param>
        /// <param name="comment">User input</param>
        /// <returns></returns>
        public bool AddComment(NeighborhoodModel neighborhood, string comment)
        {
            // If neighborhood is null, return false
            if (neighborhood == null)
            {
                return false;
            }

            // Check comment is null, return false
            if (comment == null)
            {
                return false;
            }

            // Check comment is empty, return false
            if (comment == "")
            {
                return false;
            }

            // Check to see if comments exist, if there are not, then create the list
            if (neighborhood.Comments.Count == 0)
            {
                neighborhood.Comments = new List<CommentModel>();
            }

            // Add comment to the comment list
            neighborhood.Comments.Add(
                new CommentModel()
                {
                    // Assign new Id to the CommentModel object
                    CommentId = CreateNewCommentId(),

                    // Assign the comment to the CommentModel object
                    Comment = comment
                }
            ); ;

            // Save the neighborhood
            UpdateData(neighborhood);

            return true;
        }

        /// <summary>
        /// Deletes a comment from the NeighborhoodModel.
        /// </summary>
        /// <param name="neighborhood">Current neighborhood</param>
        /// <param name="commentId">Comment's unique identifier</param>
        /// <returns></returns>
        public bool DeleteComment(NeighborhoodModel neighborhood, string commentId)
        {

            // If neighborhood is null, return false
            if (neighborhood == null)
            {
                return false;
            }

            var currentCommentList = neighborhood.Comments;
            int commentIdx = -1;

            // Search for comment to remove, store index 
            for (var i = 0; i < currentCommentList.Count; i++)
            {
                var comment = currentCommentList.ElementAt(i);
                if (comment.CommentId == commentId)
                {
                    commentIdx = i;
                    break;
                }
            }

            // Invalid ID
            if (commentIdx == -1)
                return false;

            // Remove the comment
            neighborhood.Comments.RemoveAt(commentIdx);

            // Save the neighborhood
            UpdateData(neighborhood);

            return true;
        }
    }
}