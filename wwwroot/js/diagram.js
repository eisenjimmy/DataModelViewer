// Drag and drop functionality for diagram items
let draggedElement = null;
let offsetX = 0;
let offsetY = 0;
let isDragging = false;
let dotNetRef = null;

// Function to set the DotNet reference
window.setDotNetRef = function(ref) {
    dotNetRef = ref;
};

export function initializeDragAndDrop() {
    document.addEventListener('mousedown', handleMouseDown);
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
}

function handleMouseDown(e) {
    const element = e.target.closest('.table-box, .view-box');
    if (!element) return;
    
    if (e.target.closest('input, select, textarea, button')) return;
    
    draggedElement = element;
    isDragging = true;
    
    const rect = element.getBoundingClientRect();
    offsetX = e.clientX - rect.left;
    offsetY = e.clientY - rect.top;
    
    element.style.cursor = 'grabbing';
    e.preventDefault();
}

function handleMouseMove(e) {
    if (!isDragging || !draggedElement) return;
    
    const container = document.getElementById('diagram-items');
    if (!container) return;
    
    const containerRect = container.getBoundingClientRect();
    const newX = e.clientX - containerRect.left - offsetX;
    const newY = e.clientY - containerRect.top - offsetY;
    
    draggedElement.style.left = Math.max(0, newX) + 'px';
    draggedElement.style.top = Math.max(0, newY) + 'px';
    
    // Update relationship lines
    updateRelationshipLines();
}

function handleMouseUp(e) {
    if (draggedElement) {
        draggedElement.style.cursor = 'move';
        
        // Notify Blazor about position change
        const rect = draggedElement.getBoundingClientRect();
        const container = document.getElementById('diagram-items');
        if (container) {
            const containerRect = container.getBoundingClientRect();
            const x = rect.left - containerRect.left;
            const y = rect.top - containerRect.top;
            
            const tableName = draggedElement.getAttribute('data-table-name');
            const viewName = draggedElement.getAttribute('data-view-name');
            
            if (tableName && dotNetRef) {
                dotNetRef.invokeMethodAsync('UpdateTablePosition', tableName, x, y);
            } else if (viewName && dotNetRef) {
                dotNetRef.invokeMethodAsync('UpdateViewPosition', viewName, x, y);
            }
        }
    }
    
    draggedElement = null;
    isDragging = false;
}

function updateRelationshipLines(schema) {
    if (!schema) {
        // Update based on current DOM positions
        const svg = document.getElementById('relationships-svg');
        if (!svg) return;
        
        const lines = svg.querySelectorAll('.relationship-line');
        lines.forEach(line => {
            const fromTable = line.getAttribute('data-from');
            const toTable = line.getAttribute('data-to');
            
            const fromElement = document.querySelector(`[data-table-name="${fromTable}"], [data-view-name="${fromTable}"]`);
            const toElement = document.querySelector(`[data-table-name="${toTable}"], [data-view-name="${toTable}"]`);
            
            if (fromElement && toElement) {
                const fromRect = fromElement.getBoundingClientRect();
                const toRect = toElement.getBoundingClientRect();
                const svgRect = svg.getBoundingClientRect();
                
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
    } else {
        // Update based on schema data
        const svg = document.getElementById('relationships-svg');
        if (!svg) return;
        
        const lines = svg.querySelectorAll('.relationship-line');
        lines.forEach(line => {
            const fromTable = line.getAttribute('data-from');
            const toTable = line.getAttribute('data-to');
            
            const fromElement = document.querySelector(`[data-table-name="${fromTable}"], [data-view-name="${fromTable}"]`);
            const toElement = document.querySelector(`[data-table-name="${toTable}"], [data-view-name="${toTable}"]`);
            
            if (fromElement && toElement) {
                const fromRect = fromElement.getBoundingClientRect();
                const toRect = toElement.getBoundingClientRect();
                const svgRect = svg.getBoundingClientRect();
                
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
}

// Expose function to window for Blazor interop
window.updateRelationshipLines = updateRelationshipLines;

// File download helper
window.downloadFile = function(filename, content) {
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
window.downloadPdf = function(filename, base64Content) {
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

// Initialize on page load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeDragAndDrop);
} else {
    initializeDragAndDrop();
}

