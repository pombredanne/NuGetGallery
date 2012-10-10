﻿using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using NuGet;
using NuGetGallery.Filters;
using NuGetGallery.Helpers;
using NuGetGallery.ViewModels.PackagePart;

namespace NuGetGallery.Controllers
{
	public partial class PackageFilesController : Controller
    {
		private const long MaximumAllowedPackageFileSize = 3L * 1024 * 1024;		// maximum package size = 3MB

		private readonly IPackageService packageSvc;
		private readonly IPackageFileService packageFileSvc;
		private readonly ICacheService cacheSvc;

		public PackageFilesController(IPackageService packageSvc, IPackageFileService packageFileSvc, ICacheService cacheSvc)
		{
			this.packageSvc = packageSvc;
			this.packageFileSvc = packageFileSvc;
			this.cacheSvc = cacheSvc;
		}

		public virtual ActionResult Contents(string id, string version)
		{
			Package package = packageSvc.FindPackageByIdAndVersion(id, version);
			if (package == null)
			{
				return HttpNotFound();
			}

			if (package.PackageFileSize > MaximumAllowedPackageFileSize)
			{
				return View("PackageTooBig");
			}

			IPackage packageFile = NuGetGallery.Helpers.PackageHelper.GetPackageFromCacheOrDownloadIt(package, cacheSvc, packageFileSvc);
			PackageItem rootFolder = PathToTreeConverter.Convert(packageFile.GetFiles());

			var viewModel = new PackageContentsViewModel(packageFile, rootFolder);
			return View(viewModel);
		}

		[ActionName("View")]
        [CompressFilter]
        [CacheFilter(Duration = 60 * 60 * 24)]      // cache one day
		public virtual ActionResult ShowFileContent(string id, string version, string filePath)
		{
			IPackageFile file;
			if (!TryGetPackageFile(id, version, filePath, out file))
			{
				return HttpNotFound();
			}

			// treat image files specially
			if (FileHelper.IsImageFile(file.Path))
			{
				return new ImageResult(file.GetStream(), FileHelper.GetMimeType(file.Path));
			}

			var result = new ContentResult
			{
				ContentEncoding = System.Text.Encoding.UTF8,
				ContentType = "text/plain"
			};
			
			if (FileHelper.IsBinaryFile(file.Path))
			{
				result.Content = "The requested file is a binary file.";
			}
			else
			{
				using (var reader = new StreamReader(file.GetStream()))
				{
					result.Content = reader.ReadToEnd();
				}
			}

			return result;
		}

		[ActionName("Download")]
		public virtual ActionResult DownloadFileContent(string id, string version, string filePath)
		{
			IPackageFile file;
			if (!TryGetPackageFile(id, version, filePath, out file))
			{
				return HttpNotFound();
			}

			return File(file.GetStream(), "application/octet-stream", Path.GetFileName(file.Path));
		}

		private bool TryGetPackageFile(string id, string version, string filePath, out IPackageFile packageFile)
		{
			packageFile = null;

			if (String.IsNullOrEmpty(filePath))
			{
				return false;
			}

			Package package = packageSvc.FindPackageByIdAndVersion(id, version);
			if (package == null || package.PackageFileSize > MaximumAllowedPackageFileSize)
			{
				return false;
			}

			filePath = filePath.Replace('/', Path.DirectorySeparatorChar);

			IPackage packageWithContents = NuGetGallery.Helpers.PackageHelper.GetPackageFromCacheOrDownloadIt(package, cacheSvc, packageFileSvc);

			packageFile = packageWithContents.GetFiles()
											 .FirstOrDefault(p => p.Path.Equals(filePath, StringComparison.OrdinalIgnoreCase));
			if (packageFile == null)
			{
				return false;
			}

			return true;
		}
    }
}