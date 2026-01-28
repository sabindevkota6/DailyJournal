// Quill Rich Text Editor Interop
window.quillInterop = {
    instances: {},
  
    create: function (id, readOnly, placeholder) {
        try {
       console.log(`Creating Quill editor: ${id}`);
            
      if (this.instances[id]) {
         console.log(`Quill instance ${id} already exists, destroying first`);
    this.destroy(id);
        }
            
            if (typeof Quill === 'undefined') {
     console.error('Quill is not loaded');
    return false;
          }
     
            var container = document.getElementById(id);
   if (!container) {
                console.error(`Container ${id} not found`);
        return false;
        }
 
            var existingToolbar = container.previousElementSibling;
   if (existingToolbar && existingToolbar.classList.contains('ql-toolbar')) {
 existingToolbar.remove();
            }
          
        container.innerHTML = '';
  
          this.instances[id] = new Quill(`#${id}`, {
      theme: 'snow',
   readOnly: readOnly,
             placeholder: placeholder || 'Write something...',
   modules: {
    toolbar: [
     [{ 'header': [1, 2, 3, false] }],
           ['bold', 'italic', 'underline', 'strike'],
   [{ 'list': 'ordered'}, { 'list': 'bullet' }],
         [{ 'color': [] }, { 'background': [] }],
        ['link'],
       ['clean']
         ]
       }
    });
     
            console.log(`Quill editor created: ${id}`);
      return true;
        } catch (error) {
       console.error('Error creating Quill editor:', error);
  return false;
        }
    },
    
    getHTML: function (id) {
     try {
    if (this.instances[id]) {
return this.instances[id].root.innerHTML;
            }
            return '';
  } catch (error) {
            console.error('Error getting HTML:', error);
            return '';
        }
    },
    
    setHTML: function (id, html) {
   try {
            if (this.instances[id]) {
      this.instances[id].root.innerHTML = html;
    return true;
     }
            return false;
        } catch (error) {
console.error('Error setting HTML:', error);
 return false;
 }
    },
    
    getText: function (id) {
        try {
       if (this.instances[id]) {
      return this.instances[id].getText();
         }
            return '';
        } catch (error) {
   console.error('Error getting text:', error);
            return '';
   }
    },
    
    destroy: function (id) {
        try {
          if (this.instances[id]) {
  delete this.instances[id];
           console.log(`Quill editor instance removed: ${id}`);
      return true;
      }
return false;
  } catch (error) {
        console.error('Error destroying Quill:', error);
            return false;
 }
    }
};

console.log('Quill interop loaded');
