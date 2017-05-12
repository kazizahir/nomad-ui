﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nomad.Models;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nomad.Controllers
{
    public class JobController : Controller
    {
        public static readonly string NomadUrl = Environment.GetEnvironmentVariable("NOMAD_URL");

        [Route("/jobs")]
        public async Task<IActionResult> Jobs()
        {
            var jobs = await GetJobsAsync();

            return View("~/Views/Nomad/Jobs.cshtml", jobs);
        }

        [Route("/job")]
        public async Task<IActionResult> Job(string id)
        {
            var jobTask = GetJobAsync(id);
            var jobEvaluationsTask = GetJobEvaluationsAsync(id);
            var jobAllocationsTask = GetJobAllocationsAsync(id);

            var job = await jobTask;
            job.Evaluations = await jobEvaluationsTask;
            job.Allocations = await jobAllocationsTask;

            return View("~/Views/Nomad/Job.cshtml", job);
        }

        public async Task<List<Job>> GetJobsAsync()
        {
            List<Job> jobs;

            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(NomadUrl + "/v1/jobs"))
            using (HttpContent content = response.Content)
            {
                string result = await content.ReadAsStringAsync();

                jobs = JsonConvert.DeserializeObject<List<Job>>(result);
            }

            foreach (var job in jobs)
            {
                if (job.Status == "pending") { job.Pending++; }
                if (job.Status == "running") { job.Running++; }
                if (job.Status == "dead") { job.Dead++; }
            }

            return jobs.OrderBy(j => j.ID).ToList();
        }

        public async Task<Job> GetJobAsync(string id)
        {
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(NomadUrl + "/v1/job/" + id))
            using (HttpContent content = response.Content)
            {
                string result = await content.ReadAsStringAsync();

                ViewBag.Json = JToken.Parse(result).ToString(Formatting.Indented);

                return JsonConvert.DeserializeObject<Job>(result);
            }
        }

        public async Task<List<Evaluation>> GetJobEvaluationsAsync(string id)
        {
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(NomadUrl + "/v1/job/" + id + "/evaluations"))
            using (HttpContent content = response.Content)
            {
                string result = await content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<List<Evaluation>>(result).OrderBy(e => e.JobID).ToList();
            }
        }

        public async Task<List<Allocation>> GetJobAllocationsAsync(string id)
        {
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(NomadUrl + "/v1/job/" + id + "/allocations"))
            using (HttpContent content = response.Content)
            {
                string result = await content.ReadAsStringAsync();

                var allocations = JsonConvert.DeserializeObject<List<Allocation>>(result);

                return allocations.OrderBy(a => a.Name).ToList();
            }
        }
    }
}
