/*
 * Scarlet API World Info Injector script -
 * MIT (c)2019-2020 by SirJosh3917
 * BIG thanks to SmittyW for the HTML & CSS design!
 */

const eeNeedsEnhancing = 'wi-needs-enhancing';
const eeuNeedsEnhancing = 'eeu-wi-needs-enhancing';

// themes
var themes = {
	Default: {
		bg1: '1E1E1E',
		bg2: '323232',
		txt1: 'FFFFFF',
		txt2: '999999',
		nameBorderColor: '252525'
	},
	Snow: {
		bg1: 'E1E1E1',
		bg2: 'CDCDCD',
		txt1: '000000',
		txt2: '606060',
		nameBorderColor: 'DADADA'
	}
};

themes.Carbon = themes.Default;
themes.Coal = themes.Default;
themes.Radon = themes.Default;
themes.Flax = themes.Snow;
themes.Flax_Default = themes.Default;

document.addEventListener("DOMContentLoaded", () => {
	var theme = themes[getTheme() || "Default"] || themes.Default;
  useTheme(theme);
  
  const enhanceEE = document.getElementsByClassName(eeNeedsEnhancing);
  const enhanceEEU = document.getElementsByClassName(eeuNeedsEnhancing);

  if (enhanceEE.length === 0 && enhanceEEU.length === 0) return;

  console.log("[Scarlet] Enhancing EE/U worlds.");

  for (const eeWorld of enhanceEE) {
    
  }
});

async function enhanceEE() {
  await makeGet()
}

function makeGet(url) {
  return new Promise((resolve, reject) => {
    $.ajax({
      url: url,
      success: resolve,
      error: reject
    })
  });
}

/**
 * @description creates the HTML needed for displaying a world info box
 * @parameter parentContainer the element whose inner HTML will have world-info
 * related items appended
 * @parameter world the API result about world metadata
 * @parameter details contains the worldId and scale to display the world at
 */
function createWorldInfo(parentContainer, world, details) {
	var plays = sanitize(world.Plays);
	var likes = sanitize(world.Likes);
	var favorites = sanitize(world.Favorites);
	var name = sanitize(world.Name);
	var worldId = sanitize(details.worldId);
	var owner = sanitize(world.Owner);
	var width = sanitize(world.Width);
	var height = sanitize(world.Height);
	
	// if the API supplies a UserName, then we can use that value
	// instead of making a 2nd GET request
	var ownerName;
	if (world.UserName == null) {
		ownerName = `<td name="${uniqueOwnerName(owner)}">${owner}</td>`;
		
		// update the owner text later
		callbackUpdate(world);
	} else {
		ownerName = `<td>${sanitize(world.UserName.toUpperCase())}</td>`;
	}
	
	parentContainer.innerHTML += `
<table class="wi-stats">
	<tbody>
	<tr>
		<th>P</th>
		<td>${plays}</td>
		<th><img src="${pngSrc}ee_heart.png"/></th>
		<td>${likes}</td>
		<th><img src="${pngSrc}ee_favorite.png"/></th>
		<td>${favorites}</td>
	</tr>
	</tbody>
</table>

<table class="wi-info">
	<tbody>
	<tr>
		<th>Name</th>
		<td>${name}</td>
	</tr>
	<tr>
		<th>ID</th>
		<td><a href="https://everybodyedits.com/games/${worldId}">${worldId}</a></td>
	</tr>
	<tr>
		<th>Owner</th>
		${ownerName}
	</tr>
	<tr>
		<th>Size</th>
		<td>${width} x ${height}</td>
	</tr>
	</tbody>
</table>`;
}

/**
 * @description gets the theme of the forums based on a meta tag such as the following:
 * <meta name="style" content="Radon">
 */
function getTheme() {
	var elements = document.getElementsByName("style");
	for(var element of elements) {
		if (element.localName == "meta") {
			return element.content;
		}
  }
}

/**
 * @description takes a theme and creates (or reuses) a style element in the head
 * to apply the css to
 * @parameter theme the theme to use (see up top in the script for the definition)
 */
function useTheme(theme) {
	// get the existing wi-theme element, or if it's null, make a new one
	var styleElement = document.getElementById('wi-theme') || (() => {
		var styleElement = document.createElement('style');
		styleElement.setAttribute('id', 'wi-theme');
		
		document.getElementsByTagName('head')[0]
			.appendChild(styleElement);
			
		return styleElement;
	})();
	
	if (!safeNum(theme.bg1)
		|| !safeNum(theme.bg2)
		|| !safeNum(theme.txt1)
		|| !safeNum(theme.txt2)
		|| !safeNum(theme.nameBorderColor)) {
		throw "Theme contains an item that isn't a number: " + JSON.stringify(theme);
	}
	
	styleElement.innerHTML = `
.wi-box {
	background-color: #${theme.bg1} !important;
	border-color: #${theme.bg2} !important;
	color: #${theme.txt1} !important;
}

.wi-box table {
	color: #${theme.txt1} !important;
}

.wi-map {
	border-color: #${theme.bg2} !important;
}

.wi-info th {
	background-color: #${theme.bg2} !important;
	color: #${theme.txt2} !important;
	border-color: #${theme.nameBorderColor} !important;
}

.wi-info td {
	border-color: #${theme.bg2} !important;
}

.wi-stats th {
	background-color: #${theme.bg2} !important;
}

.wi-stats td {
	border-color: #${theme.bg2} !important;
}

.wi-stats td:before {
	border-left: 7px solid #${theme.bg2} !important;
}
`;
}

/**
 * @description makes parentContainer take the place of element and
 * element becomes the child of parentContainer
 * @parameter parentContainer the new container of the element
 * @paremeter element the element to swap parents with
 */
function adoptChild(parentContainer, element) {
	// place it before the element
	element.parentNode.insertBefore(parentContainer, element);

	// change the element's parent's child (the element) to the parentContainer
	element.parentNode.replaceChild(parentContainer, element);

	// add the element to be a child of the parentContainer
	parentContainer.appendChild(element);
}

/**
 * Sanitize and encode all HTML in a user-submitted string
 * (c) 2018 Chris Ferdinandi, MIT License, https://gomakethings.com
 * @param  {String} str  The user-submitted string
 * @return {String} str  The sanitized string
 */
var sanitize = function (str) {
	var temp = document.createElement('div');
	temp.textContent = str;
	return temp.innerHTML;
};

/**
 * @description checks if a string is a safe hex string
 * @param input the string to check
 */
function safeNum(input) {
	var hexRegex = /^[0-9A-Fa-f]{6}$/g;
	
	return hexRegex.test(input);
}