// Drag and drop functionality for diagram items
// Drag and drop state
let draggedElement = null;
let offsetX = 0;
let offsetY = 0;
let isDragging = false;
let isResizing = false;
let resizeHandle = null;
let initialWidth = 0;
let initialHeight = 0;
let initialX = 0;
let initialY = 0;
let dotNetRef = null;
let animationFrameId = null;
let currentZoom = 1.0;

// Cache for drag performance
let cachedLines = [];
let cachedContainerRect = null;
let cachedSvgRect = null;

// Grid snapping
const GRID_SIZE = 20;

// Selection tracking
let mouseDownX = 0;
let mouseDownY = 0;
let hasMoved = false;
const DRAG_THRESHOLD = 5; // pixels

// Function to set the DotNet reference
window.setDotNetRef = function (ref) {
    dotNetRef = ref;
};

window.setZoom = function (zoom) {
    currentZoom = zoom;
};

// Page layout settings
let pageWidth = 32 * 96; // Default CP Custom width in pixels
let pageHeight = 36 * 96; // Default CP Custom height in pixels
const PAGE_GAP = 20; // Gap between pages

window.setPageSize = function (widthInches, heightInches) {
    pageWidth = widthInches * 96;
    pageHeight = heightInches * 96;
};

// Snap position to valid page area (not in gaps)
function snapToPageArea(x, y) {
    const totalPageWidth = pageWidth + PAGE_GAP;
    const totalPageHeight = pageHeight + PAGE_GAP;

    // Find which page cell we're in
    const col = Math.floor(x / totalPageWidth);
    const row = Math.floor(y / totalPageHeight);

    // Position within the cell
    const xInCell = x - (col * totalPageWidth);
    const yInCell = y - (row * totalPageHeight);

    // If in gap, snap to nearest page edge
    let snappedX = x;
    let snappedY = y;

    if (xInCell > pageWidth) {
        // In horizontal gap - snap to left edge of next page or right edge of current page
        const distToNextPage = totalPageWidth - xInCell;
        const distToPrevPage = xInCell - pageWidth;
        snappedX = distToNextPage < distToPrevPage
            ? (col + 1) * totalPageWidth
            : col * totalPageWidth + pageWidth - 50; // 50px from edge
    }

    if (yInCell > pageHeight) {
        // In vertical gap - snap to top edge of next page or bottom edge of current page
        const distToNextPage = totalPageHeight - yInCell;
        const distToPrevPage = yInCell - pageHeight;
        snappedY = distToNextPage < distToPrevPage
            ? (row + 1) * totalPageHeight
            : row * totalPageHeight + pageHeight - 50; // 50px from edge
    }

    return { x: snappedX, y: snappedY };
}

export function initializeDragAndDrop() {
    document.addEventListener('mousedown', handleMouseDown);
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    // Add hover listeners for highlighting
    document.body.addEventListener('mouseover', handleMouseOver);
    document.body.addEventListener('mouseout', handleMouseOut);
}

function handleMouseOver(e) {
    if (isDragging || isResizing) return;

    const element = e.target.closest('.table-box, .view-box');
    if (element) {
        const tableName = element.getAttribute('data-table-name');
        const viewName = element.getAttribute('data-view-name');
        const name = tableName || viewName;
        if (name) {
            highlightTable(name);
        }
    }
}

function handleMouseOut(e) {
    if (isDragging || isResizing) return;

    const element = e.target.closest('.table-box, .view-box');
    if (element) {
        clearHighlight();
    }
}

function highlightTable(name) {
    // 1. Dim everything first
    const allTables = document.querySelectorAll('.table-box, .view-box');
    const allLines = document.querySelectorAll('.relationship-line');

    allTables.forEach(el => el.classList.add('dimmed'));
    allLines.forEach(el => el.classList.add('dimmed'));

    // 2. Highlight the target table
    const targetElement = document.querySelector(`[data-table-name="${name}"], [data-view-name="${name}"]`);
    if (targetElement) {
        targetElement.classList.remove('dimmed');
        targetElement.classList.add('highlighted');
    }

    // 3. Find connected lines and tables
    allLines.forEach(line => {
        const from = line.getAttribute('data-from');
        const to = line.getAttribute('data-to');

        if (from === name || to === name) {
            // Highlight the line
            line.classList.remove('dimmed');
            line.classList.add('highlighted');

            // Highlight the connected table
            const otherName = (from === name) ? to : from;
            const otherElement = document.querySelector(`[data-table-name="${otherName}"], [data-view-name="${otherName}"]`);
            if (otherElement) {
                otherElement.classList.remove('dimmed');
                otherElement.classList.add('highlighted');
            }
        }
    });
}

function clearHighlight() {
    const allTables = document.querySelectorAll('.table-box, .view-box');
    const allLines = document.querySelectorAll('.relationship-line');

    allTables.forEach(el => {
        el.classList.remove('dimmed');
        el.classList.remove('highlighted');
    });

    allLines.forEach(el => {
        el.classList.remove('dimmed');
        el.classList.remove('highlighted');
    });
}

function handleMouseDown(e) {
    // Check for resize handle first
    if (e.target.classList.contains('resize-handle')) {
        const element = e.target.closest('.shape-box, .label-box');
        if (element) {
            draggedElement = element;
            isResizing = true;
            resizeHandle = e.target.getAttribute('data-handle'); // nw, ne, sw, se

            const rect = element.getBoundingClientRect();
            initialWidth = rect.width;
            initialHeight = rect.height;
            initialX = e.clientX;
            initialY = e.clientY;

            // For top/left resizing, we need current position
            const container = document.getElementById('diagram-items');
            if (container) {
                const containerRect = container.getBoundingClientRect();
                // Store current left/top relative to container
                // We can read from style.left/top if set, or calculate
                // Assuming style.left/top are always set in px
                initialLeft = parseFloat(element.style.left) || 0;
                initialTop = parseFloat(element.style.top) || 0;
            }

            e.preventDefault();
            e.stopPropagation();
            return;
        }
    }

    const element = e.target.closest('.table-box, .view-box, .label-box, .shape-box');
    if (!element) return;

    if (e.target.closest('input, select, textarea, button')) return;

    draggedElement = element;
    isDragging = true;
    hasMoved = false;
    mouseDownX = e.clientX;
    mouseDownY = e.clientY;

    // Cache container and SVG rects
    const container = document.getElementById('diagram-items');
    const svg = document.getElementById('relationships-svg');
    if (container && svg) {
        cachedContainerRect = container.getBoundingClientRect();
        cachedSvgRect = svg.getBoundingClientRect();
    }

    const rect = element.getBoundingClientRect();
    // offsetX/Y should be in unscaled coordinates relative to the element's top-left
    // (e.clientX - rect.left) is the distance in screen pixels.
    // Divide by zoom to get unscaled distance.
    offsetX = (e.clientX - rect.left) / currentZoom;
    offsetY = (e.clientY - rect.top) / currentZoom;

    // Cache connected lines for this element
    const tableName = element.getAttribute('data-table-name');
    const viewName = element.getAttribute('data-view-name');
    const name = tableName || viewName;

    cachedLines = [];
    if (name) {
        const allLines = document.querySelectorAll('.relationship-line');
        allLines.forEach(line => {
            const from = line.getAttribute('data-from');
            const to = line.getAttribute('data-to');
            if (from === name || to === name) {
                // Find the OTHER element
                const otherName = (from === name) ? to : from;
                const otherElement = document.querySelector(`[data-table-name="${otherName}"], [data-view-name="${otherName}"]`);

                if (otherElement) {
                    cachedLines.push({
                        line: line,
                        isFrom: (from === name),
                        otherElement: otherElement
                    });
                }
            }
        });
    }

    element.style.cursor = 'grabbing';
    e.preventDefault();
}

let initialLeft = 0;
let initialTop = 0;

function handleMouseMove(e) {
    if (isResizing && draggedElement) {
        if (animationFrameId) cancelAnimationFrame(animationFrameId);

        animationFrameId = requestAnimationFrame(() => {
            // Adjust delta by zoom
            const dx = (e.clientX - initialX) / currentZoom;
            const dy = (e.clientY - initialY) / currentZoom;

            let newWidth = initialWidth;
            let newHeight = initialHeight;
            let newLeft = initialLeft;
            let newTop = initialTop;

            // Handle resizing based on handle position
            if (resizeHandle === 'se') {
                newWidth = Math.max(20, initialWidth + dx);
                newHeight = Math.max(20, initialHeight + dy);

                // Snap to grid
                newWidth = Math.round(newWidth / GRID_SIZE) * GRID_SIZE;
                newHeight = Math.round(newHeight / GRID_SIZE) * GRID_SIZE;
            }

            draggedElement.style.width = newWidth + 'px';
            draggedElement.style.height = newHeight + 'px';

            // If we support other handles (nw, sw, ne), we'd update left/top too.
        });
        return;
    }

    if (!isDragging || !draggedElement) return;

    if (animationFrameId) {
        cancelAnimationFrame(animationFrameId);
    }

    animationFrameId = requestAnimationFrame(() => {
        if (!cachedContainerRect) return;

        // Calculate position relative to container, adjusting for zoom
        // The mouse position is screen coordinates.
        // The container rect is also screen coordinates (affected by zoom transform).
        // However, the internal X/Y of items are "unzoomed" coordinates.
        // So we need to calculate the delta from the offset and divide by zoom.

        // Actually, simpler approach:
        // newX = (e.clientX - cachedContainerRect.left) / currentZoom - offsetX;
        // But offsetX was calculated at start. Let's re-evaluate handleMouseDown.

        // Let's stick to the previous logic but divide by zoom?
        // No, if the container is scaled, getBoundingClientRect returns scaled values.
        // e.clientX is screen pixels.
        // So (e.clientX - cachedContainerRect.left) gives scaled pixels from left edge.
        // Divide by currentZoom to get unscaled pixels.

        let newX = (e.clientX - cachedContainerRect.left) / currentZoom - offsetX;
        let newY = (e.clientY - cachedContainerRect.top) / currentZoom - offsetY;

        // Apply grid snapping
        newX = Math.round(newX / GRID_SIZE) * GRID_SIZE;
        newY = Math.round(newY / GRID_SIZE) * GRID_SIZE;

        // Apply page-area snapping (prevent placement in gaps)
        const snapped = snapToPageArea(newX, newY);
        newX = snapped.x;
        newY = snapped.y;

        newX = Math.max(0, newX);
        newY = Math.max(0, newY);

        draggedElement.style.left = newX + 'px';
        draggedElement.style.top = newY + 'px';

        // Update ONLY connected lines using cached data
        updateCachedLines(draggedElement, newX, newY);
    });
}

function updateCachedLines(draggedEl, x, y) {
    if (!cachedSvgRect || cachedLines.length === 0) return;

    const draggedRect = draggedEl.getBoundingClientRect();
    // We can't rely on getBoundingClientRect for the dragged element during drag 
    // because it might not have updated yet or we want to use the calculated X/Y.
    // Actually, since we set style.left/top just before, we can calculate center relative to SVG.

    // Calculate center of dragged element relative to SVG
    // x, y are relative to container. SVG is typically same as container.
    // Let's assume SVG and Container are aligned (both absolute inset-0).

    const draggedCenterX = x + draggedRect.width / 2;
    const draggedCenterY = y + draggedRect.height / 2;

    cachedLines.forEach(item => {
        const otherRect = item.otherElement.getBoundingClientRect();
        const otherCenterX = otherRect.left + otherRect.width / 2 - cachedSvgRect.left;
        const otherCenterY = otherRect.top + otherRect.height / 2 - cachedSvgRect.top;

        if (item.isFrom) {
            item.line.setAttribute('x1', draggedCenterX);
            item.line.setAttribute('y1', draggedCenterY);
            item.line.setAttribute('x2', otherCenterX);
            item.line.setAttribute('y2', otherCenterY);
        } else {
            item.line.setAttribute('x1', otherCenterX);
            item.line.setAttribute('y1', otherCenterY);
            item.line.setAttribute('x2', draggedCenterX);
            item.line.setAttribute('y2', draggedCenterY);
        }
    });
}

function handleMouseUp(e) {
    if (animationFrameId) {
        cancelAnimationFrame(animationFrameId);
        animationFrameId = null;
    }

    if (isResizing && draggedElement) {
        // Notify Blazor about size change
        const rect = draggedElement.getBoundingClientRect();
        const width = rect.width;
        const height = rect.height;

        const shapeId = draggedElement.getAttribute('data-shape-id');
        const labelId = draggedElement.getAttribute('data-label-id');

        if (shapeId && dotNetRef) {
            dotNetRef.invokeMethodAsync('UpdateShapeSize', shapeId, width, height);
        } else if (labelId && dotNetRef) {
            // Labels might not need explicit size update if they are auto-sized, 
            // but if we allow resizing, we should save it.
            // For now, let's assume labels are auto-sized by content unless we add explicit size property.
            // The user asked for resizeable labels.
            dotNetRef.invokeMethodAsync('UpdateLabelSize', labelId, width, height);
        }

        isResizing = false;
        draggedElement = null;
        return;
    }

    if (draggedElement) {
        draggedElement.style.cursor = 'move';

        // Notify Blazor about position change
        if (isDragging && draggedElement) {
            // Check if it was a click (not moved significantly)
            const dx = Math.abs(e.clientX - mouseDownX);
            const dy = Math.abs(e.clientY - mouseDownY);

            if (dx < DRAG_THRESHOLD && dy < DRAG_THRESHOLD) {
                // It was a click! Select the object
                const id = draggedElement.getAttribute('data-table-name') ||
                    draggedElement.getAttribute('data-view-name') ||
                    draggedElement.getAttribute('data-shape-id') ||
                    draggedElement.getAttribute('data-label-id');

                let type = "";
                if (draggedElement.getAttribute('data-table-name')) type = "Table";
                else if (draggedElement.getAttribute('data-view-name')) type = "View";
                else if (draggedElement.getAttribute('data-shape-id')) type = "Shape";
                else if (draggedElement.getAttribute('data-label-id')) type = "Label";

                if (id && dotNetRef) {
                    dotNetRef.invokeMethodAsync('SelectObject', id, type, e.ctrlKey || e.shiftKey);
                }
            } else {
                // It was a drag - update position
                const tableName = draggedElement.getAttribute('data-table-name');
                const viewName = draggedElement.getAttribute('data-view-name');
                const shapeId = draggedElement.getAttribute('data-shape-id');
                const labelId = draggedElement.getAttribute('data-label-id');

                // Calculate position relative to container (unscaled)
                // We can use the style.left/top which we set during drag
                const x = parseFloat(draggedElement.style.left);
                const y = parseFloat(draggedElement.style.top);

                if (dotNetRef) {
                    if (tableName) dotNetRef.invokeMethodAsync('UpdateTablePosition', tableName, x, y);
                    else if (viewName) dotNetRef.invokeMethodAsync('UpdateViewPosition', viewName, x, y);
                    else if (shapeId) dotNetRef.invokeMethodAsync('UpdateShapePosition', shapeId, x, y);
                    else if (labelId) dotNetRef.invokeMethodAsync('UpdateLabelPosition', labelId, x, y);
                }
            }
        }
        // Final full update to ensure everything is correct
        updateRelationshipLines();
    } else if (!isDragging && !isResizing) {
        // Clicked on empty space (if target is container)
        if (e.target.id === 'diagram-items' || e.target.id === 'diagram-container') {
            if (dotNetRef) dotNetRef.invokeMethodAsync('DeselectAll');
        }
    }

    draggedElement = null;
    isDragging = false;
    isResizing = false;
    resizeHandle = null;
    document.body.style.cursor = 'default';
    cachedLines = [];
    cachedContainerRect = null;
    cachedSvgRect = null;
}

function updateRelationshipLines(schema) {
    // This function is still needed for initial load, window resize, or full refreshes
    const svg = document.getElementById('relationships-svg');
    if (!svg) return;

    const svgRect = svg.getBoundingClientRect();
    const lines = svg.querySelectorAll('.relationship-line');

    lines.forEach(line => {
        const fromTable = line.getAttribute('data-from');
        const toTable = line.getAttribute('data-to');

        const fromElement = document.querySelector(`[data-table-name="${fromTable}"], [data-view-name="${fromTable}"]`);
        const toElement = document.querySelector(`[data-table-name="${toTable}"], [data-view-name="${toTable}"]`);

        if (fromElement && toElement) {
            const fromRect = fromElement.getBoundingClientRect();
            const toRect = toElement.getBoundingClientRect();

            const fromX = fromRect.left + fromRect.width / 2 - svgRect.left;
            const fromY = fromRect.top + fromRect.height / 2 - svgRect.top;
            const toX = toRect.left + toRect.width / 2 - svgRect.left;
            const toY = toRect.top + toRect.height / 2 - svgRect.top;

            line.setAttribute('x1', fromX);
            line.setAttribute('y1', fromY);
            line.setAttribute('x2', toX);
            line.setAttribute('y2', toY);
        }
    });
}

// Expose function to window for Blazor interop
window.updateRelationshipLines = updateRelationshipLines;

window.toggleDarkMode = function (isDark) {
    if (isDark) {
        document.documentElement.classList.add('dark');
    } else {
        document.documentElement.classList.remove('dark');
    }
};

// File download helper
window.downloadFile = function (filename, content) {
    const blob = new Blob([content], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
};

// PDF download helper
window.downloadPdf = function (filename, base64Content) {
    const byteCharacters = atob(base64Content);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: 'application/pdf' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
};

window.initParticles = function () {
    if (window.particlesJS) {
        particlesJS('particles-js', {
            "particles": {
                "number": {
                    "value": 150,
                    "density": {
                        "enable": true,
                        "value_area": 800
                    }
                },
                "color": {
                    "value": "#3b82f6"
                },
                "shape": {
                    "type": "circle",
                    "stroke": {
                        "width": 0,
                        "color": "#000000"
                    },
                    "polygon": {
                        "nb_sides": 5
                    }
                },
                "opacity": {
                    "value": 0.5,
                    "random": false,
                    "anim": {
                        "enable": false,
                        "speed": 1,
                        "opacity_min": 0.1,
                        "sync": false
                    }
                },
                "size": {
                    "value": 3,
                    "random": true,
                    "anim": {
                        "enable": false,
                        "speed": 40,
                        "size_min": 0.1,
                        "sync": false
                    }
                },
                "line_linked": {
                    "enable": true,
                    "distance": 150,
                    "color": "#3b82f6",
                    "opacity": 0.4,
                    "width": 1
                },
                "move": {
                    "enable": true,
                    "speed": 2,
                    "direction": "none",
                    "random": false,
                    "straight": false,
                    "out_mode": "out",
                    "bounce": false,
                    "attract": {
                        "enable": false,
                        "rotateX": 600,
                        "rotateY": 1200
                    }
                }
            },
            "interactivity": {
                "detect_on": "canvas",
                "events": {
                    "onhover": {
                        "enable": true,
                        "mode": "grab"
                    },
                    "onclick": {
                        "enable": true,
                        "mode": "push"
                    },
                    "resize": true
                },
                "modes": {
                    "grab": {
                        "distance": 140,
                        "line_linked": {
                            "opacity": 1
                        }
                    },
                    "bubble": {
                        "distance": 400,
                        "size": 40,
                        "duration": 2,
                        "opacity": 8,
                        "speed": 3
                    },
                    "repulse": {
                        "distance": 200,
                        "duration": 0.4
                    },
                    "push": {
                        "particles_nb": 4
                    },
                    "remove": {
                        "particles_nb": 2
                    }
                }
            },
            "retina_detect": true
        });
    }
};

// Initialize on page load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeDragAndDrop);
} else {
    initializeDragAndDrop();
}
