//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


// prototypes for function pointers

/// <summary>
///     Callback prototype for the resolve call.
///		CoApp-engine should call this for each package that needs to be installed
///		And should call this in the order that it discovers packages, with the assumtion
///     that the last package specified is the first packages to be installed.
/// </summary>
/// <param name="package_name">
///		the name of the package to add to the list that needs to be installed
/// </param>
/// <param name="local_path">
///		The local file path of the package (if it exists locally). 
///		If the package must be downloaded, this should be NULL.
/// </param>
/// <param name="url">
///		The URL of the package to be downloaded. If the local_path is not null, this 
///		can be null (it shall never be used if the package is already local)
/// </param>
/// <returns>
///     the callee shall return:
///			0 on success.
///			1 on user reqested cancel. 
/// </returns>
typedef int (CoAppResolveCallback)(const wchar_t* package_name, const wchar_t* local_path, const wchar_t* url);

/// <summary>
///     Callback prototype for the Install call.
///		CoApp-engine should call periodically during the install of a package
/// </summary>
/// <param name="current_message">
///		A plain text message that MAY be shown to the user
/// </param>
/// <param name="install_status">
///		an integer representing the stage or status of the app being installed 
///		(exact definition TBA)
/// </param>
/// <param name="percent_complete">
///		an integer between 0 and 100 indicating the current percent complete.  
/// </param>
/// <returns>
///     the callee shall return:
///			0 on success.
///			1 on user reqested cancel. 
/// </returns>
typedef int (CoAppInstallCallback)(const wchar_t* current_message, int install_status,int percent_complete );

/// <summary>
///     Callback prototype for the Download call.
///		CoApp-engine should call periodically during the download of a file
/// </summary>
/// <param name="current_message">
///		A plain text message that MAY be shown to the user
/// </param>
/// <param name="download_status">
///		an integer representing the stage or status of the file being downloaded
///		(exact definition TBA)
/// </param>
/// <param name="bytes_downloaded">
///		the number of bytes downloaded  
/// </param>
/// <param name="percent_complete">
///		the total number of bytes expected
/// </param>
/// <returns>
///     the callee shall return:
///			0 on success.
///			1 on user reqested cancel. 
/// </returns>
typedef int (CoAppDownloadProgressHandler)(const wchar_t* current_message, int download_status, __int64 bytes_downloaded, __int64 total_bytes  );

/// <summary>
///     Resolves the list of packages to be installed to satisfy the request
/// </summary>
/// <param name="package_path">
///		Either a URL or a local file.
///		When passed a URL:
///			the engine must resolve the target file (download it) and proceed 
///			based on what kind of a file it is.
///			
///		Based on the file type (once it's local)		
///			if the file is an MSI, this is taken as the package to be installed 
///
///			if the file is an XML document, this is taken as a package feed 
///			to install all the items in the feed.
///
/// </param>
/// <param name="callback">
///		Resolve should call this callback once for every package to be installed.
/// </param>
/// <returns>
///     [TBA RETURN CODE?]
///			0 on success.
///			1 on user reqested cancel. 
/// </returns> 
typedef int (coapp_resolve_prototype)(const wchar_t* package_path, CoAppResolveCallback* callback );

/// <summary>
///     Downloads a file from a URL to a local path.
/// </summary>
/// <param name="package_url">
///		a url with the location of the file to be downloaded
/// </param>
/// <param name="local_path">
///		the location the downloaded file should be placed when complete
/// </param>
/// <param name="callback">
///		Resolve should call this callback during the download to provide status and the ability to cancel the download.
/// </param>
/// <returns>
///     [TBA RETURN CODE?]
///			0 on success.
///			1 on user reqested cancel. 
/// </returns> 
typedef int (coapp_download_prototype)(const wchar_t* package_url, const wchar_t* local_path, CoAppDownloadProgressHandler* callback );

/// <summary>
///     Installs a package from a local file.
/// </summary>
/// <param name="local_path">
///		the path of the file on a local file system
/// </param>
/// <param name="callback">
///		Resolve should call this callback during the install to provide status and the ability to cancel the download.
/// </param>
/// <returns>
///     [TBA RETURN CODE?]
///			0 on success.
///			1 on user reqested cancel. 
/// </returns> 
typedef int (coapp_install_prototype)(const wchar_t* local_path, CoAppInstallCallback* callback);
